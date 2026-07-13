<?php

declare(strict_types=1);

const GENERATOR_VERSION = 'search-v1';

use Nexus\Search\Application\Aggregator\SearchAggregator;
use Nexus\Search\Application\Plan\SearchPlanItem;
use Nexus\Search\Application\ProviderExecution\SequentialProviderSearchExecutor;
use Nexus\Search\Domain\Exception\InvalidSearchTerm;
use Nexus\Search\Domain\Exception\InvalidYearRange;
use Nexus\Search\Domain\Exception\UnknownProviderAlias;
use Nexus\Search\Domain\Port\AcademicProviderPort;
use Nexus\Search\Domain\Port\AdapterCollection;
use Nexus\Search\Domain\Port\DeduplicationPort;
use Nexus\Search\Domain\Port\SearchCachePort;
use Nexus\Search\Domain\SearchQuery;
use Nexus\Search\Domain\SearchTerm;
use Nexus\Search\Domain\YearRange;
use Nexus\Search\Infrastructure\Plan\YamlSearchPlanParser;
use Nexus\Shared\Domain\CorpusSlice;
use Nexus\Shared\Domain\ScholarlyWork;
use Nexus\Shared\ValueObject\LanguageCode;
use Nexus\Shared\ValueObject\WorkId;
use Nexus\Shared\ValueObject\WorkIdNamespace;
use Nexus\Shared\ValueObject\WorkIdSet;

$options = getopt('', ['php-reference:', 'source-lock:', 'input:', 'comparison:', 'output:', 'manifest:']);
foreach (['php-reference', 'source-lock', 'input', 'comparison', 'output', 'manifest'] as $required) {
    if (!isset($options[$required]) || !is_string($options[$required])) {
        fwrite(STDERR, "Missing --{$required}.\n");
        exit(2);
    }
}

$reference = realpath($options['php-reference']);
if ($reference === false) {
    fwrite(STDERR, "PHP reference path does not exist.\n");
    exit(2);
}

$sourceLockBytes = readRequired($options['source-lock'], 'source lock');
$sourceLock = json_decode($sourceLockBytes, true, flags: JSON_THROW_ON_ERROR);
$sourceRepository = $sourceLock['php_reference']['repository'] ?? null;
$sourceCommit = $sourceLock['php_reference']['commit'] ?? null;
if (!is_string($sourceRepository) || !is_string($sourceCommit)) {
    throw new RuntimeException('Source lock does not contain a valid php_reference repository and commit.');
}

$head = trim(runGit($reference, ['rev-parse', 'HEAD']));
if ($head !== $sourceCommit) {
    fwrite(STDERR, "Refusing generation: expected {$sourceCommit}, found {$head}.\n");
    exit(3);
}
if (trim(runGit($reference, ['status', '--porcelain', '--untracked-files=no'])) !== '') {
    fwrite(STDERR, "Refusing generation from a dirty PHP reference worktree.\n");
    exit(3);
}

spl_autoload_register(static function (string $class) use ($reference): void {
    $prefix = 'Nexus\\';
    if (!str_starts_with($class, $prefix)) {
        return;
    }
    $path = $reference.'/src/'.str_replace('\\', '/', substr($class, strlen($prefix))).'.php';
    if (is_file($path)) {
        require_once $path;
    }
});

$inputBytes = readRequired($options['input'], 'fixture input');
$comparisonBytes = readRequired($options['comparison'], 'semantic classifications');
$input = json_decode($inputBytes, true, flags: JSON_THROW_ON_ERROR);
$results = [];
foreach ($input['cases'] as $case) {
    $results[] = ['id' => $case['id'], 'operation' => $case['operation'], 'result' => executeCase($case, $input['validationYear'])];
}

$output = [
    'fixtureSetId' => $input['fixtureSetId'],
    'schemaVersion' => '1.0.0',
    'sourceKind' => 'pinned-php-observable-behavior',
    'sourceCommit' => $sourceCommit,
    'cases' => $results,
];
$outputBytes = encodeJson($output);
writeFile($options['output'], $outputBytes);

$manifest = [
    'fixtureSetId' => $input['fixtureSetId'],
    'schemaVersion' => '1.0.0',
    'sourceKind' => 'pinned-php-observable-behavior',
    'sourceRepository' => $sourceRepository,
    'sourceCommit' => $sourceCommit,
    'sourceRefs' => [
        'src/Search/Domain/SearchTerm.php',
        'src/Search/Domain/YearRange.php',
        'src/Search/Domain/SearchQuery.php',
        'src/Search/Domain/Port/AdapterCollection.php',
        'src/Search/Application/ProviderExecution/SequentialProviderSearchExecutor.php',
        'src/Search/Application/Aggregator/SearchAggregator.php',
        'src/Search/Infrastructure/Plan/YamlSearchPlanParser.php',
        'tests/Unit/Search/SearchQueryTest.php',
        'tests/Unit/Search/Application/Aggregator/SearchAggregatorTest.php',
        'tests/Unit/Search/Application/ProviderExecution/SequentialProviderSearchExecutorTest.php',
        'tests/Unit/Search/Application/Plan/SearchPlanParserTest.php',
    ],
    'generatorCommand' => 'php scripts/php-golden/search-export.php --php-reference "$PHP_REFERENCE" --source-lock specs/SOURCE.lock.json --input fixtures/php-golden/search/v1/input.json --comparison fixtures/php-golden/search/v1/comparison.json --output fixtures/php-golden/search/v1/expected.json --manifest fixtures/php-golden/search/v1/manifest.json',
    'generatorVersion' => GENERATOR_VERSION,
    'environmentAssumptions' => [
        'PHP 8.3 or later',
        'git is available',
        'PHP reference tracked files are clean',
        'no network access or Composer dependencies are required',
        'future-year rejection uses a far-future value independent of wall-clock year',
        'runtime ids and durations are excluded from generated output',
        'UTF-8 JSON with LF line endings',
    ],
    'inputDigest' => 'sha256:'.hash('sha256', $inputBytes),
    'outputDigest' => 'sha256:'.hash('sha256', $outputBytes),
    'sourceLockDigest' => 'sha256:'.hash('sha256', $sourceLockBytes),
    'classificationDigest' => 'sha256:'.hash('sha256', $comparisonBytes),
    'ignoredNondeterminism' => ['generated query ids', 'provider durations', 'wall-clock max-year message text'],
    'comparisonRules' => [
        'compare normalized validation categories rather than language-specific exception classes',
        'compare cache field sensitivity and equality relations rather than cache-key byte equality',
        'compare provider registration and result order exactly',
        'exclude runtime durations and generated query ids',
        'require every case to have a reviewed semantic classification',
    ],
];
writeFile($options['manifest'], encodeJson($manifest));

function executeCase(array $case, int $validationYear): array
{
    return match ($case['operation']) {
        'query-term' => queryTerm($case),
        'year-range' => yearRange($case, $validationYear),
        'provider-alias-normalization' => providerAliasNormalization($case),
        'cache-provider-order' => cacheProviderOrder($case),
        'cache-field-sensitivity' => cacheFieldSensitivity($case),
        'cache-include-raw-data' => cacheIncludeRawData($case),
        'legacy-plan-import' => legacyPlanImport($case),
        'authoritative-plan-unknown-field' => authoritativePlanUnknownField($case),
        'provider-selection' => providerSelection($case),
        'provider-execution' => providerExecution($case),
        'search-time-deduplication' => searchTimeDeduplication($case),
        default => throw new InvalidArgumentException("Unknown operation {$case['operation']}"),
    };
}

function queryTerm(array $case): array
{
    try {
        $term = new SearchTerm($case['value']);
        return ['accepted' => true, 'value' => $term->value];
    } catch (InvalidSearchTerm) {
        return ['accepted' => false, 'errorCategory' => 'query-length-below-minimum'];
    }
}

function yearRange(array $case, int $validationYear): array
{
    try {
        $range = new YearRange($case['from'], $case['to']);
        return ['accepted' => true, 'from' => $range->from, 'to' => $range->to];
    } catch (InvalidYearRange $error) {
        $maxYear = $validationYear + 5;
        if ($case['from'] !== null && ($case['from'] < 1000 || $case['from'] > $maxYear)) {
            $category = $case['from'] < 1000 ? 'year-from-below-minimum' : 'year-from-exceeds-validation-year';
        } elseif ($case['to'] !== null && ($case['to'] < 1000 || $case['to'] > $maxYear)) {
            $category = $case['to'] < 1000 ? 'year-to-below-minimum' : 'year-to-exceeds-validation-year';
        } else {
            $category = 'year-range-inverted';
        }
        return ['accepted' => false, 'errorCategory' => $category];
    }
}

function providerAliasNormalization(array $case): array
{
    $query = new SearchQuery(new SearchTerm('AI'), providerAliases: $case['aliases'], id: 'Q_fixture');
    return ['aliases' => $query->providerAliases];
}

function cacheProviderOrder(array $case): array
{
    $query = makeQuery($case['request']);
    $first = $query->cacheKey($case['providersA']);
    $second = $query->cacheKey($case['providersB']);
    return ['firstKey' => $first, 'secondKey' => $second, 'equal' => $first === $second];
}

function cacheFieldSensitivity(array $case): array
{
    $base = $case['request'];
    $providers = $case['providers'];
    $baseKey = makeQuery($base)->cacheKey($providers);
    $languageKey = makeQuery(array_replace($base, ['language' => 'fr']))->cacheKey($providers);
    $maxKey = makeQuery(array_replace($base, ['maxResults' => $base['maxResults'] + 1]))->cacheKey($providers);
    $offsetKey = makeQuery(array_replace($base, ['offset' => $base['offset'] + 1]))->cacheKey($providers);
    $providerKey = makeQuery($base)->cacheKey(['crossref']);
    return [
        'baseKey' => $baseKey,
        'languageChangesKey' => $baseKey !== $languageKey,
        'maxResultsChangesKey' => $baseKey !== $maxKey,
        'offsetChangesKey' => $baseKey !== $offsetKey,
        'providersChangeKey' => $baseKey !== $providerKey,
    ];
}

function cacheIncludeRawData(array $case): array
{
    $without = makeQuery(array_replace($case['request'], ['includeRawData' => false]))->cacheKey($case['providers']);
    $with = makeQuery(array_replace($case['request'], ['includeRawData' => true]))->cacheKey($case['providers']);
    return ['withoutRawKey' => $without, 'withRawKey' => $with, 'equal' => $without === $with];
}

function legacyPlanImport(array $case): array
{
    $plan = (new YamlSearchPlanParser)->parseArray($case['plan'], 'fixture.json');
    return planResult($plan->projectId, $plan->items);
}

function authoritativePlanUnknownField(array $case): array
{
    $plan = (new YamlSearchPlanParser)->parseArray($case['plan'], 'fixture.json');
    return ['accepted' => true, 'itemCount' => count($plan->items)];
}

function planResult(string $projectId, array $items): array
{
    return [
        'projectId' => $projectId,
        'items' => array_map(static fn (SearchPlanItem $item): array => [
            'id' => $item->id,
            'query' => $item->query,
            'projectId' => $item->projectId,
            'maxResults' => $item->maxResults,
            'yearFrom' => $item->yearFrom,
            'yearTo' => $item->yearTo,
            'providerAliases' => $item->providerAliases,
            'includeRawData' => $item->includeRawData,
            'sourceIndex' => $item->sourceIndex,
        ], $items),
    ];
}

function providerSelection(array $case): array
{
    $adapters = array_map(static fn (string $alias): AcademicProviderPort => makeProvider(['alias' => $alias, 'works' => []]), $case['registered']);
    try {
        $selected = (new AdapterCollection(...$adapters))->matching($case['selected']);
        return ['accepted' => true, 'aliases' => array_map(static fn (AcademicProviderPort $p): string => $p->alias(), $selected)];
    } catch (UnknownProviderAlias) {
        return ['accepted' => false, 'errorCategory' => 'unknown-provider-alias'];
    }
}

function providerExecution(array $case): array
{
    $providers = array_map(static fn (array $definition): AcademicProviderPort => makeProvider($definition), $case['providers']);
    $result = (new SequentialProviderSearchExecutor)->execute(makeQuery(['query' => 'search', 'includeRawData' => false]), $providers);
    return [
        'stats' => array_map(static fn ($providerResult): array => [
            'alias' => $providerResult->alias,
            'status' => $providerResult->succeeded() ? 'success' : 'failure',
            'resultCount' => $providerResult->stat->resultCount,
            'skipReason' => $providerResult->stat->skipReason,
        ], $result->results),
        'workIds' => array_map(static fn (ScholarlyWork $work): ?string => $work->primaryId()?->toString(), $result->works()),
        'allFailed' => $result->results !== [] && array_reduce($result->results, static fn (bool $carry, $item): bool => $carry && !$item->succeeded(), true),
    ];
}

function searchTimeDeduplication(array $case): array
{
    $providers = array_map(static fn (array $definition): AcademicProviderPort => makeProvider($definition), $case['providers']);
    $deduplication = new class implements DeduplicationPort {
        public function deduplicate(CorpusSlice $corpus): CorpusSlice { return $corpus; }
    };
    $cache = new class implements SearchCachePort {
        public function get(string $key): ?array { return null; }
        public function put(string $key, array $works, int $ttlSeconds = 3600): void {}
        public function has(string $key): bool { return false; }
        public function invalidateAll(): void {}
    };
    $result = (new SearchAggregator(new AdapterCollection(...$providers), $deduplication, $cache))->aggregate(makeQuery(['query' => 'search', 'includeRawData' => false]));
    return ['totalRaw' => $result->totalRaw, 'outputCount' => $result->corpus->count(), 'outputKind' => 'deduplicated-corpus'];
}

function makeQuery(array $request): SearchQuery
{
    $yearRange = isset($request['yearFrom']) || isset($request['yearTo'])
        ? new YearRange($request['yearFrom'] ?? null, $request['yearTo'] ?? null)
        : null;
    $language = isset($request['language']) && $request['language'] !== null ? new LanguageCode($request['language']) : null;
    return new SearchQuery(
        new SearchTerm($request['query']),
        yearRange: $yearRange,
        language: $language,
        maxResults: $request['maxResults'] ?? 100,
        offset: $request['offset'] ?? 0,
        includeRawData: $request['includeRawData'] ?? false,
        id: 'Q_fixture',
    );
}

function makeProvider(array $definition): AcademicProviderPort
{
    $works = array_map(static fn (array $work): ScholarlyWork => makeWork($work, $definition['alias']), $definition['works'] ?? []);
    return new class($definition['alias'], $works, $definition['failure'] ?? null) implements AcademicProviderPort {
        public function __construct(private string $providerAlias, private array $works, private ?string $failure) {}
        public function alias(): string { return $this->providerAlias; }
        public function search(SearchQuery $query): array {
            if ($this->failure !== null) { throw new RuntimeException($this->failure); }
            return $this->works;
        }
        public function fetchById(WorkId $id): ?ScholarlyWork { return null; }
        public function supports(WorkIdNamespace $ns): bool { return true; }
    };
}

function makeWork(array $definition, string $provider): ScholarlyWork
{
    return ScholarlyWork::reconstitute(
        ids: new WorkIdSet(new WorkId(WorkIdNamespace::DOI, $definition['doi'])),
        title: $definition['title'],
        sourceProvider: $provider,
    );
}

function readRequired(string $path, string $description): string
{
    $bytes = file_get_contents($path);
    if ($bytes === false) { throw new RuntimeException("Unable to read {$description}."); }
    return $bytes;
}

function encodeJson(array $value): string
{
    return json_encode($value, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE | JSON_THROW_ON_ERROR)."\n";
}

function writeFile(string $path, string $bytes): void
{
    $directory = dirname($path);
    if (!is_dir($directory) && !mkdir($directory, 0777, true) && !is_dir($directory)) { throw new RuntimeException("Unable to create {$directory}."); }
    if (file_put_contents($path, $bytes) === false) { throw new RuntimeException("Unable to write {$path}."); }
}

function runGit(string $workingDirectory, array $arguments): string
{
    $pipes = [];
    $process = proc_open(['git', '-C', $workingDirectory, ...$arguments], [1 => ['pipe', 'w'], 2 => ['pipe', 'w']], $pipes);
    if (!is_resource($process)) { throw new RuntimeException('Unable to start git.'); }
    $stdout = stream_get_contents($pipes[1]);
    $stderr = stream_get_contents($pipes[2]);
    fclose($pipes[1]); fclose($pipes[2]);
    if (proc_close($process) !== 0) { throw new RuntimeException("git failed: {$stderr}"); }
    return $stdout;
}
