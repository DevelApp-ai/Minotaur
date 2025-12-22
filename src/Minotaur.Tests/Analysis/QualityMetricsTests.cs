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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minotaur.Analysis;

namespace Minotaur.Tests.Analysis;

[TestClass]
public class QualityMetricsTests
{
    [TestMethod]
    public void QualityMetrics_WithRequiredProperties_CreatesInstance()
    {
        // Arrange & Act
        var metrics = new QualityMetrics
        {
            Accuracy = 0.95,
            Precision = 0.92,
            Recall = 0.88,
            F1Score = 0.90
        };

        // Assert
        Assert.IsNotNull(metrics);
        Assert.AreEqual(0.95, metrics.Accuracy);
        Assert.AreEqual(0.92, metrics.Precision);
        Assert.AreEqual(0.88, metrics.Recall);
        Assert.AreEqual(0.90, metrics.F1Score);
    }

    [TestMethod]
    public void QualityMetrics_WithConfidence_StoresValue()
    {
        // Arrange & Act
        var metrics = new QualityMetrics
        {
            Accuracy = 0.85,
            Precision = 0.80,
            Recall = 0.75,
            F1Score = 0.77,
            Confidence = 0.92
        };

        // Assert
        Assert.IsNotNull(metrics.Confidence);
        Assert.AreEqual(0.92, metrics.Confidence.Value);
    }

    [TestMethod]
    public void QualityMetrics_WithCustomMetrics_StoresAdditionalData()
    {
        // Arrange & Act
        var metrics = new QualityMetrics
        {
            Accuracy = 0.90,
            Precision = 0.85,
            Recall = 0.88,
            F1Score = 0.86,
            CustomMetrics = new Dictionary<string, double>
            {
                ["specificity"] = 0.93,
                ["sensitivity"] = 0.91,
                ["mcc"] = 0.84
            }
        };

        // Assert
        Assert.IsNotNull(metrics.CustomMetrics);
        Assert.AreEqual(3, metrics.CustomMetrics.Count);
        Assert.AreEqual(0.93, metrics.CustomMetrics["specificity"]);
        Assert.AreEqual(0.91, metrics.CustomMetrics["sensitivity"]);
        Assert.AreEqual(0.84, metrics.CustomMetrics["mcc"]);
    }

    [TestMethod]
    public void QualityMetrics_PerfectScores_AllOnes()
    {
        // Arrange & Act
        var metrics = new QualityMetrics
        {
            Accuracy = 1.0,
            Precision = 1.0,
            Recall = 1.0,
            F1Score = 1.0,
            Confidence = 1.0
        };

        // Assert
        Assert.AreEqual(1.0, metrics.Accuracy);
        Assert.AreEqual(1.0, metrics.Precision);
        Assert.AreEqual(1.0, metrics.Recall);
        Assert.AreEqual(1.0, metrics.F1Score);
        Assert.AreEqual(1.0, metrics.Confidence);
    }

    [TestMethod]
    public void QualityMetrics_ZeroScores_AllZeros()
    {
        // Arrange & Act
        var metrics = new QualityMetrics
        {
            Accuracy = 0.0,
            Precision = 0.0,
            Recall = 0.0,
            F1Score = 0.0,
            Confidence = 0.0
        };

        // Assert
        Assert.AreEqual(0.0, metrics.Accuracy);
        Assert.AreEqual(0.0, metrics.Precision);
        Assert.AreEqual(0.0, metrics.Recall);
        Assert.AreEqual(0.0, metrics.F1Score);
        Assert.AreEqual(0.0, metrics.Confidence);
    }

    [TestMethod]
    public void QualityMetrics_WithoutOptionalProperties_IsValid()
    {
        // Arrange & Act
        var metrics = new QualityMetrics
        {
            Accuracy = 0.75,
            Precision = 0.70,
            Recall = 0.72,
            F1Score = 0.71
        };

        // Assert
        Assert.IsNull(metrics.Confidence);
        Assert.IsNull(metrics.CustomMetrics);
    }

    [TestMethod]
    public void QualityMetrics_SameValues_PropertiesMatch()
    {
        // Arrange
        var metrics1 = new QualityMetrics
        {
            Accuracy = 0.85,
            Precision = 0.80,
            Recall = 0.82,
            F1Score = 0.81
        };

        var metrics2 = new QualityMetrics
        {
            Accuracy = 0.85,
            Precision = 0.80,
            Recall = 0.82,
            F1Score = 0.81
        };

        // Act & Assert
        Assert.AreEqual(metrics1.Accuracy, metrics2.Accuracy);
        Assert.AreEqual(metrics1.Precision, metrics2.Precision);
        Assert.AreEqual(metrics1.Recall, metrics2.Recall);
        Assert.AreEqual(metrics1.F1Score, metrics2.F1Score);
    }

    [TestMethod]
    public void QualityMetrics_DifferentValues_PropertiesDiffer()
    {
        // Arrange
        var metrics1 = new QualityMetrics
        {
            Accuracy = 0.85,
            Precision = 0.80,
            Recall = 0.82,
            F1Score = 0.81
        };

        var metrics2 = new QualityMetrics
        {
            Accuracy = 0.90,
            Precision = 0.85,
            Recall = 0.87,
            F1Score = 0.86
        };

        // Act & Assert
        Assert.AreNotEqual(metrics1.Accuracy, metrics2.Accuracy);
        Assert.AreNotEqual(metrics1.Precision, metrics2.Precision);
    }

    [TestMethod]
    public void QualityMetrics_CustomMetricsEmpty_IsValid()
    {
        // Arrange & Act
        var metrics = new QualityMetrics
        {
            Accuracy = 0.88,
            Precision = 0.85,
            Recall = 0.86,
            F1Score = 0.855,
            CustomMetrics = new Dictionary<string, double>()
        };

        // Assert
        Assert.IsNotNull(metrics.CustomMetrics);
        Assert.AreEqual(0, metrics.CustomMetrics.Count);
    }

    [TestMethod]
    public void QualityMetrics_NegativeValues_Allowed()
    {
        // Arrange & Act - Some metrics might use negative values in specific contexts
        var metrics = new QualityMetrics
        {
            Accuracy = -0.5,
            Precision = -0.3,
            Recall = -0.2,
            F1Score = -0.25
        };

        // Assert
        Assert.AreEqual(-0.5, metrics.Accuracy);
        Assert.AreEqual(-0.3, metrics.Precision);
    }

    [TestMethod]
    public void QualityMetrics_ValuesAboveOne_Allowed()
    {
        // Arrange & Act - Some custom metrics might exceed 1.0
        var metrics = new QualityMetrics
        {
            Accuracy = 1.5,
            Precision = 1.2,
            Recall = 1.3,
            F1Score = 1.25
        };

        // Assert
        Assert.AreEqual(1.5, metrics.Accuracy);
        Assert.AreEqual(1.2, metrics.Precision);
    }
}
