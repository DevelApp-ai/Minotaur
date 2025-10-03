/*
 * This file is part of Minotaur.
 * 
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Minotaur is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with Minotaur. If not, see <https://www.gnu.org/licenses/>.
 */

namespace Minotaur.Projects.Grammar.Detectors;

/// <summary>
/// Composite grammar detector that combines multiple detection strategies to provide the best possible grammar detection.
/// This detector runs multiple child detectors and selects the result with the highest confidence.
/// </summary>
public class CompositeGrammarDetector : IGrammarDetector
{
    private readonly List<IGrammarDetector> _detectors;
    private readonly bool _requireConsensus;
    private readonly double _minimumConfidence;

    /// <summary>
    /// Initializes a new instance of the CompositeGrammarDetector class.
    /// </summary>
    /// <param name="detectors">The collection of detectors to use.</param>
    /// <param name="requireConsensus">If true, requires multiple detectors to agree on the result.</param>
    /// <param name="minimumConfidence">The minimum confidence level required for a result to be accepted.</param>
    public CompositeGrammarDetector(
        IEnumerable<IGrammarDetector> detectors,
        bool requireConsensus = false,
        double minimumConfidence = 0.5)
    {
        _detectors = detectors.OrderByDescending(d => d.Priority).ToList();
        _requireConsensus = requireConsensus;
        _minimumConfidence = Math.Clamp(minimumConfidence, 0.0, 1.0);
    }

    /// <summary>
    /// Gets the detector identifier.
    /// </summary>
    public string DetectorId => "composite";

    /// <summary>
    /// Gets the priority of this detector (highest priority).
    /// </summary>
    public int Priority => 1000;

    /// <summary>
    /// Detects grammar using multiple detection strategies.
    /// </summary>
    /// <param name="context">The detection context.</param>
    /// <returns>A task that represents the asynchronous detection operation.</returns>
    public async Task<GrammarDetectionResult> DetectGrammarAsync(GrammarDetectionContext context)
    {
        if (!CanDetect(context))
        {
            return GrammarDetectionResult.Failure(
                "No detectors available or context is invalid",
                DetectorId);
        }

        var results = new List<(IGrammarDetector detector, GrammarDetectionResult result)>();
        var metadata = new Dictionary<string, object>
        {
            ["detectionMethod"] = "composite",
            ["detectorsUsed"] = new List<string>(),
            ["allResults"] = new List<object>()
        };

        // Run all applicable detectors
        foreach (var detector in _detectors.Where(d => d.CanDetect(context)))
        {
            try
            {
                var result = await detector.DetectGrammarAsync(context);
                results.Add((detector, result));

                ((List<string>)metadata["detectorsUsed"]).Add(detector.DetectorId);
                ((List<object>)metadata["allResults"]).Add(new
                {
                    DetectorId = detector.DetectorId,
                    IsSuccessful = result.IsSuccessful,
                    GrammarName = result.GrammarName,
                    Confidence = result.Confidence,
                    FailureReason = result.FailureReason
                });
            }
            catch (Exception ex)
            {
                // Log the error but continue with other detectors
                ((List<object>)metadata["allResults"]).Add(new
                {
                    DetectorId = detector.DetectorId,
                    IsSuccessful = false,
                    Error = ex.Message
                });
            }
        }

        if (!results.Any())
        {
            return GrammarDetectionResult.Failure(
                "No detectors could process the context",
                DetectorId,
                metadata);
        }

        // Filter successful results
        var successfulResults = results
            .Where(r => r.result.IsSuccessful && r.result.Confidence >= _minimumConfidence)
            .ToList();

        if (!successfulResults.Any())
        {
            metadata["reason"] = "No results met minimum confidence threshold";
            return GrammarDetectionResult.Failure(
                $"No results met minimum confidence threshold of {_minimumConfidence}",
                DetectorId,
                metadata);
        }

        if (_requireConsensus)
        {
            return await GetConsensusResultAsync(successfulResults, metadata);
        }
        else
        {
            return GetBestResultAsync(successfulResults, metadata);
        }
    }

    /// <summary>
    /// Determines if this detector can handle the given context.
    /// </summary>
    /// <param name="context">The detection context.</param>
    /// <returns>True if at least one child detector can handle the context, false otherwise.</returns>
    public bool CanDetect(GrammarDetectionContext context)
    {
        return _detectors.Any(d => d.CanDetect(context));
    }

    /// <summary>
    /// Adds a detector to the composite.
    /// </summary>
    /// <param name="detector">The detector to add.</param>
    public void AddDetector(IGrammarDetector detector)
    {
        _detectors.Add(detector);
        _detectors.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    /// <summary>
    /// Removes a detector from the composite.
    /// </summary>
    /// <param name="detectorId">The ID of the detector to remove.</param>
    /// <returns>True if the detector was removed, false if it wasn't found.</returns>
    public bool RemoveDetector(string detectorId)
    {
        var detector = _detectors.FirstOrDefault(d => d.DetectorId == detectorId);
        if (detector != null)
        {
            _detectors.Remove(detector);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets all registered detectors.
    /// </summary>
    /// <returns>A read-only list of detectors.</returns>
    public IReadOnlyList<IGrammarDetector> GetDetectors()
    {
        return _detectors.AsReadOnly();
    }

    private async Task<GrammarDetectionResult> GetConsensusResultAsync(
        List<(IGrammarDetector detector, GrammarDetectionResult result)> results,
        Dictionary<string, object> metadata)
    {
        // Group results by grammar name
        var grammarGroups = results
            .GroupBy(r => r.result.GrammarName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Find the grammar with the most votes
        var consensusGroup = grammarGroups
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Average(r => r.result.Confidence))
            .First();

        if (consensusGroup.Count() < 2)
        {
            metadata["consensusReason"] = "No consensus reached - using best single result";
            return GetBestResultAsync(results, metadata);
        }

        // Calculate weighted average confidence
        var totalWeight = consensusGroup.Sum(r => r.result.Confidence);
        var weightedConfidence = consensusGroup.Sum(r => r.result.Confidence * r.result.Confidence) / totalWeight;

        // Combine all versions and fallbacks
        var versions = consensusGroup
            .Select(r => r.result.Version)
            .Where(v => v != null)
            .Distinct()
            .ToList();

        var allFallbacks = consensusGroup
            .SelectMany(r => r.result.FallbackGrammars)
            .Distinct()
            .ToList();

        metadata["consensusCount"] = consensusGroup.Count();
        metadata["consensusConfidence"] = weightedConfidence;
        metadata["agreementRatio"] = (double)consensusGroup.Count() / results.Count;

        var bestResult = consensusGroup.OrderByDescending(r => r.result.Confidence).First().result;

        await Task.CompletedTask;
        return GrammarDetectionResult.Success(
            bestResult.GrammarName!,
            versions.FirstOrDefault(),
            weightedConfidence,
            DetectorId,
            metadata,
            allFallbacks);
    }

    private GrammarDetectionResult GetBestResultAsync(
        List<(IGrammarDetector detector, GrammarDetectionResult result)> results,
        Dictionary<string, object> metadata)
    {
        // Select the result with the highest confidence, breaking ties by detector priority
        var bestResult = results
            .OrderByDescending(r => r.result.Confidence)
            .ThenByDescending(r => r.detector.Priority)
            .First();

        metadata["bestDetector"] = bestResult.detector.DetectorId;
        metadata["bestConfidence"] = bestResult.result.Confidence;
        metadata["selectionMethod"] = "highest-confidence";

        // Merge metadata from the best result
        foreach (var kvp in bestResult.result.Metadata)
        {
            if (!metadata.ContainsKey(kvp.Key))
            {
                metadata[kvp.Key] = kvp.Value;
            }
        }

        return GrammarDetectionResult.Success(
            bestResult.result.GrammarName!,
            bestResult.result.Version,
            bestResult.result.Confidence,
            DetectorId,
            metadata,
            bestResult.result.FallbackGrammars);
    }

    /// <summary>
    /// Creates a default composite detector with standard detection strategies.
    /// </summary>
    /// <param name="requireConsensus">Whether to require consensus between detectors.</param>
    /// <param name="minimumConfidence">The minimum confidence threshold.</param>
    /// <returns>A configured composite grammar detector.</returns>
    public static CompositeGrammarDetector CreateDefault(bool requireConsensus = false, double minimumConfidence = 0.5)
    {
        return new CompositeGrammarDetector(new IGrammarDetector[]
        {
            new ContentBasedGrammarDetector(),
            new FileExtensionGrammarDetector()
        }, requireConsensus, minimumConfidence);
    }
}