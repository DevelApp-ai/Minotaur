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
using Minotaur.Validation;

namespace Minotaur.Tests.Validation;

[TestClass]
public class ValidationResultTests
{
    [TestMethod]
    public void ValidationResult_WithErrorSeverity_CreatesErrorResult()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Message = "This is an error",
            Severity = ValidationSeverity.Error
        };

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("This is an error", result.Message);
        Assert.AreEqual(ValidationSeverity.Error, result.Severity);
    }

    [TestMethod]
    public void ValidationResult_WithWarningSeverity_CreatesWarningResult()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Message = "This is a warning",
            Severity = ValidationSeverity.Warning
        };

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("This is a warning", result.Message);
        Assert.AreEqual(ValidationSeverity.Warning, result.Severity);
    }

    [TestMethod]
    public void ValidationResult_WithLocation_StoresLocationInformation()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Message = "Error at location",
            Severity = ValidationSeverity.Error,
            Location = new SourcePosition
            {
                Line = 10,
                Column = 5,
                File = "test.cs"
            }
        };

        // Assert
        Assert.IsNotNull(result.Location);
        Assert.AreEqual(10, result.Location.Line);
        Assert.AreEqual(5, result.Location.Column);
        Assert.AreEqual("test.cs", result.Location.File);
    }

    [TestMethod]
    public void ValidationResult_WithCode_StoresErrorCode()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Message = "Syntax error",
            Severity = ValidationSeverity.Error,
            Code = "CS001"
        };

        // Assert
        Assert.AreEqual("CS001", result.Code);
    }

    [TestMethod]
    public void ValidationResult_WithContext_StoresContextInformation()
    {
        // Arrange & Act
        var context = new Dictionary<string, string>
        {
            ["rule"] = "identifier",
            ["expected"] = "alphanumeric"
        };

        var result = new ValidationResult
        {
            Message = "Invalid identifier",
            Severity = ValidationSeverity.Error,
            Context = context
        };

        // Assert
        Assert.IsNotNull(result.Context);
        Assert.AreEqual(2, result.Context.Count);
        Assert.AreEqual("identifier", result.Context["rule"]);
        Assert.AreEqual("alphanumeric", result.Context["expected"]);
    }

    [TestMethod]
    public void ValidationResult_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var result1 = new ValidationResult
        {
            Message = "Same error",
            Severity = ValidationSeverity.Error
        };

        var result2 = new ValidationResult
        {
            Message = "Same error",
            Severity = ValidationSeverity.Error
        };

        // Act & Assert
        Assert.AreEqual(result1, result2);
    }

    [TestMethod]
    public void ValidationSeverity_AllLevels_CanBeCreated()
    {
        // Arrange & Act
        var info = new ValidationResult { Message = "Info", Severity = ValidationSeverity.Info };
        var warning = new ValidationResult { Message = "Warning", Severity = ValidationSeverity.Warning };
        var error = new ValidationResult { Message = "Error", Severity = ValidationSeverity.Error };
        var critical = new ValidationResult { Message = "Critical", Severity = ValidationSeverity.Critical };

        // Assert
        Assert.AreEqual(ValidationSeverity.Info, info.Severity);
        Assert.AreEqual(ValidationSeverity.Warning, warning.Severity);
        Assert.AreEqual(ValidationSeverity.Error, error.Severity);
        Assert.AreEqual(ValidationSeverity.Critical, critical.Severity);
    }

    [TestMethod]
    public void SourcePosition_Constructor_SetsProperties()
    {
        // Arrange & Act
        var position = new SourcePosition
        {
            Line = 42,
            Column = 7,
            File = "program.cs"
        };

        // Assert
        Assert.AreEqual(42, position.Line);
        Assert.AreEqual(7, position.Column);
        Assert.AreEqual("program.cs", position.File);
    }

    [TestMethod]
    public void SourcePosition_WithoutFile_IsValid()
    {
        // Arrange & Act
        var position = new SourcePosition
        {
            Line = 1,
            Column = 1
        };

        // Assert
        Assert.AreEqual(1, position.Line);
        Assert.AreEqual(1, position.Column);
        Assert.IsNull(position.File);
    }

    [TestMethod]
    public void ValidationResult_WithAllProperties_CreatesCompleteResult()
    {
        // Arrange & Act
        var result = new ValidationResult
        {
            Message = "Complete validation error",
            Severity = ValidationSeverity.Critical,
            Location = new SourcePosition { Line = 100, Column = 25 },
            Code = "VAL999",
            Context = new Dictionary<string, string> { ["detail"] = "test" }
        };

        // Assert
        Assert.AreEqual("Complete validation error", result.Message);
        Assert.AreEqual(ValidationSeverity.Critical, result.Severity);
        Assert.IsNotNull(result.Location);
        Assert.AreEqual("VAL999", result.Code);
        Assert.IsNotNull(result.Context);
    }
}
