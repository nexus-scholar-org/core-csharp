<?php

declare(strict_types=1);

const GENERATOR_VERSION = 'citation-export-observations-v1';

use Nexus\CitationNetwork\Application\Builder\CitationGraphBuilder;
use Nexus\CitationNetwork\Domain\CitationGraph;
use Nexus\CitationNetwork\Domain\CitationGraphType;
use Nexus\CitationNetwork\Domain\Exception\WorkNotInGraph;
use Nexus\Dissemination\Application\Support\ValidatesExportFilename;
use Nexus\Dissemination\Domain\BibliographyFormat;
use Nexus\Dissemination\Domain\NetworkExportFormat;
use Nexus\Dissemination\Infrastructure\Serializer\BibTexSerializer;
use Nexus\Dissemination\Infrastructure\Serializer\GraphMlSerializer;
use Nexus\Shared\Domain\CorpusSlice;
use Nexus\Shared\Domain\ScholarlyWork;
use Nexus\Shared\ValueObject\Author;
use Nexus\Shared\ValueObject\AuthorList;
use Nexus\Shared\ValueObject\Venue;
use Nexus\Shared\ValueObject\WorkId;
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

$status = trim(runGit($reference, ['status', '--porcelain', '--untracked-files=no']));
if ($status !== '') {
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
    $results[] = [
        'id' => $case['id'],
        'operation' => $case['operation'],
        'result' => executeCase($case),
    ];
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
        'src/CitationNetwork/Application/Builder/CitationGraphBuilder.php',
        'src/CitationNetwork/Domain/CitationGraph.php',
        'src/CitationNetwork/Domain/CitationGraphType.php',
        'src/CitationNetwork/Domain/CitationLink.php',
        'src/Dissemination/Application/Support/ValidatesExportFilename.php',
        'src/Dissemination/Domain/BibliographyFormat.php',
        'src/Dissemination/Domain/NetworkExportFormat.php',
        'src/Dissemination/Infrastructure/Serializer/BibTexSerializer.php',
        'src/Dissemination/Infrastructure/Serializer/GraphMlSerializer.php',
        'src/Shared/Domain/CorpusSlice.php',
        'src/Shared/Domain/ScholarlyWork.php',
        'src/Shared/ValueObject/Author.php',
        'src/Shared/ValueObject/AuthorList.php',
        'src/Shared/ValueObject/Venue.php',
        'src/Shared/ValueObject/WorkId.php',
        'src/Shared/ValueObject/WorkIdNamespace.php',
        'src/Shared/ValueObject/WorkIdSet.php',
        'tests/Unit/CitationNetwork/Application/Builder/CitationGraphBuilderTest.php',
        'tests/Unit/CitationNetwork/Domain/CitationGraphTest.php',
        'tests/Unit/Dissemination/Infrastructure/BibTexSerializerTest.php',
        'tests/Unit/Dissemination/Infrastructure/GraphMlSerializerTest.php',
    ],
    'generatorCommand' => 'php scripts/php-golden/citation-export-observations.php --php-reference "$PHP_REFERENCE" --source-lock specs/SOURCE.lock.json --input fixtures/php-golden/citation-export/v1/input.json --comparison fixtures/php-golden/citation-export/v1/comparison.json --output fixtures/php-golden/citation-export/v1/expected.json --manifest fixtures/php-golden/citation-export/v1/manifest.json',
    'generatorVersion' => GENERATOR_VERSION,
    'environmentAssumptions' => [
        'PHP 8.3 or later',
        'git is available',
        'PHP reference tracked files are clean',
        'no network access or Composer dependencies are required',
        'no Mbsoft graph dependencies are required for fixture generation',
        'no Laravel persistence path is exercised',
        'runtime IDs, timestamps, object hashes, and persistence state are excluded from generated outputs',
        'UTF-8 JSON with LF line endings',
    ],
    'inputDigest' => 'sha256:'.hash('sha256', $inputBytes),
    'outputDigest' => 'sha256:'.hash('sha256', $outputBytes),
    'sourceLockDigest' => 'sha256:'.hash('sha256', $sourceLockBytes),
    'classificationDigest' => 'sha256:'.hash('sha256', $comparisonBytes),
    'ignoredNondeterminism' => [
        'generated citation graph ids',
        'generated corpus slice ids',
        'retrieved timestamps in ScholarlyWork',
    ],
    'comparisonRules' => [
        'compare PHP observable behavior only, not any C# replay targets',
        'compare normalized outputs derived from deterministic fixed-case inputs',
        'ignore runtime object identity, storage paths, and internal IDs',
    ],
];
writeFile($options['manifest'], encodeJson($manifest));

function executeCase(array $case): array
{
    return match ($case['operation']) {
        'graph-type-vocabulary' => graphTypeVocabulary(),
        'bibliography-format-vocabulary' => bibliographyFormatVocabulary(),
        'network-export-format-vocabulary' => networkExportFormatVocabulary(),
        'namespace-qualified-nodes' => namespaceQualifiedNodes($case),
        'missing-citing-node-rejected' => missingCitingNodeRejected($case),
        'external-cited-node-accepted' => externalCitedNodeAccepted($case),
        'duplicate-normalized-edge-deduplicated' => duplicateNormalizedEdgeDeduplicated($case),
        'direct-builder-known-references-only' => directBuilderKnownReferencesOnly($case),
        'co-citation-weighted-pairs' => coCitationWeightedPairs($case),
        'bibliographic-coupling-weighted-pairs' => bibliographicCouplingWeightedPairs($case),
        'bibtex-article' => bibtexFromCase($case),
        'bibtex-preprint' => bibtexFromCase($case),
        'graphml-corpus-nodes-and-escaping' => graphmlCorpusNodesAndEscaping($case),
        'export-filename-extension-validation' => exportFilenameValidation($case),
        default => throw new InvalidArgumentException("Unknown operation {$case['operation']}"),
    };
}

function graphTypeVocabulary(): array
{
    return [
        'values' => array_map(
            static fn (CitationGraphType $type): string => $type->value,
            CitationGraphType::cases(),
        ),
    ];
}

function bibliographyFormatVocabulary(): array
{
    return [
        'formats' => array_map(
            static function (BibliographyFormat $format): array {
                return [
                    'name' => $format->value,
                    'extension' => $format->extension(),
                    'mimeType' => $format->mimeType(),
                ];
            },
            BibliographyFormat::cases(),
        ),
    ];
}

function networkExportFormatVocabulary(): array
{
    return [
        'formats' => array_map(
            static function (NetworkExportFormat $format): array {
                return [
                    'name' => $format->value,
                    'extension' => $format->extension(),
                    'mimeType' => $format->mimeType(),
                ];
            },
            NetworkExportFormat::cases(),
        ),
    ];
}

function namespaceQualifiedNodes(array $case): array
{
    $graph = new CitationGraphBuilder();
    $works = makeWorks($case['works']);
    $built = $graph->buildDirectCitationGraph('fixture-namespace', $works, []);
    $nodes = [];

    foreach ($built->allWorks() as $work) {
        $nodes[] = [
            'primaryId' => $work->primaryId()?->toString(),
            'ids' => normalizeWorkIdList($work->ids()->all()),
            'title' => $work->title(),
        ];
    }

    usort($nodes, static function (array $left, array $right): int {
        return $left['primaryId'] <=> $right['primaryId'];
    });

    return [
        'nodeCount' => $built->nodeCount(),
        'nodes' => $nodes,
    ];
}

function missingCitingNodeRejected(array $case): array
{
    $graph = CitationGraph::create(CitationGraphType::CITATION, 'fixture-missing');
    $citing = WorkId::fromString($case['citingId']);
    $cited = WorkId::fromString($case['citedId']);

    try {
        $graph->recordCitation($citing, $cited);
        return [
            'rejected' => false,
            'errorCategory' => 'expected-rejection-missing',
            'note' => 'missing citing node was not rejected',
        ];
    } catch (WorkNotInGraph $error) {
        return [
            'rejected' => true,
            'errorCategory' => 'missing-citing-node',
            'error' => $error->getMessage(),
        ];
    }
}

function externalCitedNodeAccepted(array $case): array
{
    $source = makeWork($case['sourceWork']);
    $external = WorkId::fromString($case['externalCitedId']);
    $graph = CitationGraph::create(CitationGraphType::CITATION, 'fixture-external');
    $graph->addWork($source);
    $graph->recordCitation($source->primaryId(), $external);

    return [
        'accepted' => true,
        'externalIsGraphNode' => $graph->hasWork($external),
        'edgeCount' => $graph->edgeCount(),
        'edges' => normalizeCitationEdges($graph->allEdges()),
    ];
}

function duplicateNormalizedEdgeDeduplicated(array $case): array
{
    $builder = new CitationGraphBuilder();
    $graph = $builder->buildDirectCitationGraph(
        'fixture-duplicate-edge',
        makeWorks($case['works']),
        normalizeReferenceMap($case['referencesByWorkId']),
    );

    return [
        'edgeCount' => $graph->edgeCount(),
        'edges' => normalizeCitationEdges($graph->allEdges()),
    ];
}

function directBuilderKnownReferencesOnly(array $case): array
{
    $builder = new CitationGraphBuilder();
    $graph = $builder->buildDirectCitationGraph(
        'fixture-known-refs',
        makeWorks($case['works']),
        normalizeReferenceMap($case['referencesByWorkId']),
    );

    return [
        'edgeCount' => $graph->edgeCount(),
        'edges' => normalizeCitationEdges($graph->allEdges()),
    ];
}

function coCitationWeightedPairs(array $case): array
{
    $builder = new CitationGraphBuilder();
    $graph = $builder->buildCoCitationGraph(
        'fixture-co-citation',
        makeWorks($case['works']),
        normalizeReferenceMap($case['referencesByCitingWorkId']),
        normalizeReferenceMap($case['citingWorkIdsByCitedWorkId'] ?? []),
    );

    return [
        'nodeCount' => $graph->nodeCount(),
        'edgeCount' => $graph->edgeCount(),
        'edges' => normalizeCitationEdges($graph->allEdges()),
        'directed' => false,
    ];
}

function bibliographicCouplingWeightedPairs(array $case): array
{
    $builder = new CitationGraphBuilder();
    $graph = $builder->buildBibliographicCouplingGraph(
        'fixture-coupling',
        makeWorks($case['works']),
        normalizeReferenceMap($case['referencesByWorkId']),
    );

    return [
        'nodeCount' => $graph->nodeCount(),
        'edgeCount' => $graph->edgeCount(),
        'edges' => normalizeCitationEdges($graph->allEdges()),
    ];
}

function bibtexFromCase(array $case): array
{
    $serializer = new BibTexSerializer();
    $content = $serializer->serialize(CorpusSlice::fromWorks(...makeWorks([$case['work']])));

    return [
        'format' => $case['format'],
        'content' => $content,
    ];
}

function graphmlCorpusNodesAndEscaping(array $case): array
{
    $serializer = new GraphMlSerializer();
    $content = $serializer->serialize(CorpusSlice::fromWorks(...makeWorks($case['works'])));

    return [
        'nodeCount' => count($case['works']),
        'content' => $content,
    ];
}

function exportFilenameValidation(array $case): array
{
    $validator = new class {
        use ValidatesExportFilename;

        public function validate(string $filename, string $extension, string $format): array
        {
            try {
                $this->assertFilenameMatchesExtension($filename, $extension, $format);
                return ['passed' => true];
            } catch (InvalidArgumentException $error) {
                return ['passed' => false, 'errorCategory' => 'extension-mismatch', 'error' => $error->getMessage()];
            }
        }
    };

    $results = [];
    foreach ($case['checks'] as $check) {
        $results[] = [
            'format' => $check['format'],
            'filename' => $check['filename'],
            'extension' => $check['extension'],
            'result' => $validator->validate($check['filename'], $check['extension'], $check['format']),
        ];
    }

    return ['checks' => $results];
}

function makeWorks(array $definitions): array
{
    $works = [];
    foreach ($definitions as $definition) {
        $works[] = makeWork($definition);
    }

    return $works;
}

function makeWork(array $definition): ScholarlyWork
{
    return ScholarlyWork::reconstitute(
        ids: makeWorkIdSet($definition['ids']),
        title: $definition['title'],
        sourceProvider: $definition['sourceProvider'],
        year: $definition['year'] ?? null,
        authors: makeAuthors($definition['authors'] ?? []),
        venue: makeVenue($definition['venue'] ?? null),
        abstract: $definition['abstract'] ?? null,
        citedByCount: $definition['citedByCount'] ?? null,
        isRetracted: $definition['isRetracted'] ?? false,
    );
}

function makeWorkIdSet(array $definitions): WorkIdSet
{
    return WorkIdSet::fromArray(array_map(static fn (string $raw): WorkId => WorkId::fromString($raw), $definitions));
}

function makeAuthors(array $definitions): AuthorList
{
    if ($definitions === []) {
        return AuthorList::empty();
    }

    $authors = [];
    foreach ($definitions as $definition) {
        $authors[] = new Author(
            $definition['familyName'],
            $definition['givenName'] ?? null,
            null,
            $definition['normalizedFullName'] ?? null,
        );
    }

    return AuthorList::fromArray($authors);
}

function makeVenue(array|null $definition): ?Venue
{
    if ($definition === null) {
        return null;
    }

    return new Venue(
        $definition['name'],
        $definition['issn'] ?? null,
        $definition['type'] ?? null,
        $definition['publisher'] ?? null,
    );
}

function normalizeReferenceMap(array $referenceMap): array
{
    foreach ($referenceMap as $source => $references) {
        if (!is_array($references)) {
            throw new InvalidArgumentException('Reference map values must be arrays.');
        }
    }

    return $referenceMap;
}

function normalizeCitationEdges(array $edges): array
{
    $normalized = [];
    foreach ($edges as $edge) {
        $normalized[] = [
            'from' => $edge->citing->toString(),
            'to' => $edge->cited->toString(),
            'weight' => normalizeNumeric($edge->weight),
        ];
    }

    usort($normalized, static function (array $left, array $right): int {
        return [$left['from'], $left['to'], (string) $left['weight']] <=> [$right['from'], $right['to'], (string) $right['weight']];
    });

    return $normalized;
}

function normalizeWorkIdList(array $ids): array
{
    $normalized = array_map(static fn (WorkId $id): string => $id->toString(), $ids);
    sort($normalized, SORT_STRING);

    return array_values($normalized);
}

function normalizeNumeric(float $value): int|float
{
    return $value === (int) $value ? (int) $value : $value;
}

function readRequired(string $path, string $description): string
{
    $bytes = file_get_contents($path);
    if ($bytes === false) {
        throw new RuntimeException("Unable to read {$description}.");
    }

    return $bytes;
}

function encodeJson(array $value): string
{
    return json_encode($value, JSON_PRETTY_PRINT | JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE | JSON_THROW_ON_ERROR)."\n";
}

function writeFile(string $path, string $bytes): void
{
    $directory = dirname($path);
    if (!is_dir($directory) && !mkdir($directory, 0777, true) && !is_dir($directory)) {
        throw new RuntimeException("Unable to create {$directory}.");
    }

    if (file_put_contents($path, $bytes) === false) {
        throw new RuntimeException("Unable to write {$path}.");
    }
}

function runGit(string $workingDirectory, array $arguments): string
{
    $command = ['git', '-C', $workingDirectory, ...$arguments];
    $pipes = [];
    $process = proc_open($command, [1 => ['pipe', 'w'], 2 => ['pipe', 'w']], $pipes);
    if (!is_resource($process)) {
        throw new RuntimeException('Unable to start git.');
    }

    $stdout = stream_get_contents($pipes[1]);
    $stderr = stream_get_contents($pipes[2]);
    fclose($pipes[1]);
    fclose($pipes[2]);
    $exitCode = proc_close($process);
    if ($exitCode !== 0) {
        throw new RuntimeException("git failed: {$stderr}");
    }

    return $stdout;
}
