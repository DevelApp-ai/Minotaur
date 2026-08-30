using Minotaur.Core;
using Minotaur.Parser;
using Xunit;

namespace Minotaur.Tests.Grammar;

/// <summary>
/// Tests for Classic ASP grammar parsing
/// Validates that the enhanced grammar handles all ASP constructs including COM, built-in objects, and edge cases
/// </summary>
public class ClassicAspGrammarTests
{
    private readonly StepParserIntegration _integration;

    public ClassicAspGrammarTests()
    {
        _integration = new StepParserIntegration();
    }

    #region Basic ASP Structure Tests

    [Fact]
    public async Task Parse_ShouldHandleSimpleAspPage()
    {
        var sourceCode = "<html><body><% Response.Write(\"Hello\") %></body></html>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
        Assert.NotEmpty(result.Children);
    }

    [Fact]
    public async Task Parse_ShouldHandleAspDirective()
    {
        var sourceCode = "<%@ LANGUAGE=VBScript %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleCodeBlocks()
    {
        var sourceCode = "<% Dim x : x = 10 %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleExpressions()
    {
        var sourceCode = "<%= userName %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleServerSideInclude()
    {
        var sourceCode = "<!--#include file=\"header.asp\" -->";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    #endregion

    #region Built-in ASP Objects Tests

    [Fact]
    public async Task Parse_ShouldHandleRequestObject()
    {
        var sourceCode = "<% userId = Request.QueryString(\"id\") %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleRequestForm()
    {
        var sourceCode = "<% userName = Request.Form(\"name\") %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleRequestCookies()
    {
        var sourceCode = "<% sessionId = Request.Cookies(\"session\") %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleRequestServerVariables()
    {
        var sourceCode = "<% method = Request.ServerVariables(\"REQUEST_METHOD\") %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleResponseObject()
    {
        var sourceCode = "<% Response.Write(\"Hello\") %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleResponseRedirect()
    {
        var sourceCode = "<% Response.Redirect(\"/login.asp\") %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleSessionObject()
    {
        var sourceCode = "<% Session(\"UserName\") = \"John\" %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleServerObject()
    {
        var sourceCode = "<% path = Server.MapPath(\"/images\") %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleApplicationObject()
    {
        var sourceCode = "<% Application.Lock() %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    #endregion

    #region COM Interop Tests

    [Fact]
    public async Task Parse_ShouldHandleServerCreateObject()
    {
        var sourceCode = "<% Set conn = Server.CreateObject(\"ADODB.Connection\") %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleADOConnection()
    {
        var sourceCode = "<% Dim conn : Set conn = Server.CreateObject(\"ADODB.Connection\") %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleADORecordset()
    {
        var sourceCode = "<% Set rs = Server.CreateObject(\"ADODB.Recordset\") %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleADOCommand()
    {
        var sourceCode = "<% Set cmd = Server.CreateObject(\"ADODB.Command\") %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleFileSystemObject()
    {
        var sourceCode = "<% Set fso = Server.CreateObject(\"Scripting.FileSystemObject\") %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleCustomComObject()
    {
        var sourceCode = "<% Set obj = Server.CreateObject(\"MyCompany.MyComponent\") %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Parse_ShouldHandleOnErrorResumeNext()
    {
        var sourceCode = "<% On Error Resume Next %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleOnErrorGoTo()
    {
        var sourceCode = "<% On Error GoTo ErrorHandler %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleErrObject()
    {
        var sourceCode = "<% errorCode = Err.Number %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleErrDescription()
    {
        var sourceCode = "<% errorDesc = Err.Description %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    #endregion

    #region VBScript Constructs Tests

    [Fact]
    public async Task Parse_ShouldHandleVariableDeclaration()
    {
        var sourceCode = "<% Dim userName As String %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleMultipleVariableDeclaration()
    {
        var sourceCode = "<% Dim userName As String, userId As Integer %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandlePrivatePublicVariables()
    {
        var sourceCode = "<% Private myVar : Public sharedVar %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleVBScriptTypes()
    {
        var sourceCode = "<% Dim str As String, num As Integer, dbl As Double, bln As Boolean, dt As Date %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleAssignment()
    {
        var sourceCode = "<% userName = \"John\" %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleSetAssignment()
    {
        var sourceCode = "<% Set obj = Server.CreateObject(\"ADODB.Connection\") %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleIfStatement()
    {
        var sourceCode = @"<% If userId <> \"\" Then Response.Write(\"Valid\") End If %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleIfElseStatement()
    {
        var sourceCode = @"<% If x > 0 Then Response.Write(\"Positive\") Else Response.Write(\"Zero or Negative\") End If %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleIfElseIfStatement()
    {
        var sourceCode = @"<% If x > 0 Then Response.Write(\"Positive\") ElseIf x < 0 Then Response.Write(\"Negative\") Else Response.Write(\"Zero\") End If %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleForLoop()
    {
        var sourceCode = "<% For i = 1 To 10 : Response.Write(i) : Next %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleForLoopWithStep()
    {
        var sourceCode = "<% For i = 1 To 10 Step 2 : Response.Write(i) : Next %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleWhileLoop()
    {
        var sourceCode = "<% While i < 10 : Response.Write(i) : i = i + 1 : Wend %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleDoLoop()
    {
        var sourceCode = "<% Do While i < 10 : Response.Write(i) : i = i + 1 : Loop %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleSelectCase()
    {
        var sourceCode = @"<% Select Case userType Case \"Admin\" : Response.Write(\"Admin\") Case \"User\" : Response.Write(\"User\") Case Else : Response.Write(\"Unknown\") End Select %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleWithStatement()
    {
        var sourceCode = @"<% With Request : userId = .QueryString(\"id\") : End With %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleFunctionCall()
    {
        var sourceCode = "<% myFunction() %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleCallStatement()
    {
        var sourceCode = "<% Call myFunction() %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleVBScriptComment()
    {
        var sourceCode = "<% ' This is a comment %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    #endregion

    #region JScript Constructs Tests

    [Fact]
    public async Task Parse_ShouldHandleJScriptVariableDeclaration()
    {
        var sourceCode = "<% var userName = \"John\"; %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleJScriptMultipleVariables()
    {
        var sourceCode = "<% var userName = \"John\", userId = 123; %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleJScriptAssignment()
    {
        var sourceCode = "<% userName = \"John\"; %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleJScriptIfStatement()
    {
        var sourceCode = "<% if (userId != \"\") { Response.Write(\"Valid\"); } %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleJScriptIfElseStatement()
    {
        var sourceCode = "<% if (x > 0) { Response.Write(\"Positive\"); } else { Response.Write(\"Zero or Negative\"); } %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleJScriptForLoop()
    {
        var sourceCode = "<% for (var i = 0; i < 10; i++) { Response.Write(i); } %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleJScriptWhileLoop()
    {
        var sourceCode = "<% while (i < 10) { Response.Write(i); i++; } %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleJScriptTryCatch()
    {
        var sourceCode = "<% try { Response.Write(\"Test\"); } catch (e) { Response.Write(\"Error: \" + e); } %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleJScriptSwitch()
    {
        var sourceCode = "<% switch (userType) { case \"Admin\": Response.Write(\"Admin\"); break; default: Response.Write(\"Unknown\"); } %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleJScriptWithStatement()
    {
        var sourceCode = "<% with (Request) { var userId = QueryString(\"id\"); } %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleJScriptCommentSingleLine()
    {
        var sourceCode = "<% // This is a comment %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleJScriptCommentMultiLine()
    {
        var sourceCode = "<% /* This is a
               multi-line comment */ %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    #endregion

    #region Literal Tests

    [Fact]
    public async Task Parse_ShouldHandleStringLiterals()
    {
        var sourceCode = "<% userName = \"John\" %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleSingleQuoteStrings()
    {
        var sourceCode = "<% userName = 'John' %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleIntegerLiterals()
    {
        var sourceCode = "<% userId = 123 %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleDecimalLiterals()
    {
        var sourceCode = "<% price = 19.99 %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleBooleanLiterals()
    {
        var sourceCode = "<% isValid = True %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleNullLiterals()
    {
        var sourceCode = "<% userName = Nothing %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleHexLiterals()
    {
        var sourceCode = "<% color = &HFF0000 %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleOctalLiterals()
    {
        var sourceCode = "<% permissions = &O755 %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleDateLiterals()
    {
        var sourceCode = "<% startDate = #01/01/2024# %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    #endregion

    #region Operator Tests

    [Fact]
    public async Task Parse_ShouldHandleArithmeticOperators()
    {
        var sourceCode = "<% result = a + b - c * d / e %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleComparisonOperators()
    {
        var sourceCode = "<% if a = b or a <> b or a > b or a < b Then %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleLogicalOperators()
    {
        var sourceCode = "<% if a And b Or c Xor d Then %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleStringConcatenation()
    {
        var sourceCode = "<% fullName = firstName & \" \" & lastName %>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    #endregion

    #region Embedded Language Tests

    [Fact]
    public async Task Parse_ShouldHandleHtmlWithEmbeddedAsp()
    {
        var sourceCode = @"<html>
<head><title>Test</title></head>
<body>
<h1>Hello</h1>
<% Response.Write(\"World\") %>
</body>
</html>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleHtmlWithJavaScriptAndAsp()
    {
        var sourceCode = @"<html>
<head>
<script>
function test() { return true; }
</script>
</head>
<body>
<% Response.Write(\"Test\") %>
</body>
</html>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleHtmlWithCssAndAsp()
    {
        var sourceCode = @"<html>
<head>
<style>
body { color: red; }
</style>
</head>
<body>
<% Response.Write(\"Test\") %>
</body>
</html>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    #endregion

    #region Complex Scenario Tests

    [Fact]
    public async Task Parse_ShouldHandleCompleteAspPage()
    {
        var sourceCode = @"<%@ LANGUAGE=VBScript %>
<% Option Explicit %>
<html>
<head><title>User List</title></head>
<body>
<%
Dim conn, rs, sql
Set conn = Server.CreateObject(\"ADODB.Connection\")
conn.Open \"myConnectionString\"
Set rs = Server.CreateObject(\"ADODB.Recordset\")

sql = \"SELECT * FROM Users\"
rs.Open sql, conn

If Not rs.EOF Then
    Do Until rs.EOF
        Response.Write(\"<div>\" & rs(\"UserName\") & \"</div>\")
        rs.MoveNext
    Loop
Else
    Response.Write(\"No users found\")
End If

rs.Close
conn.Close
Set rs = Nothing
Set conn = Nothing
%>
</body>
</html>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleAspWithErrorHandling()
    {
        var sourceCode = @"<%
On Error Resume Next
Dim conn
Set conn = Server.CreateObject(\"ADODB.Connection\")
If Err.Number <> 0 Then
    Response.Write(\"Error: \" & Err.Description)
    Err.Clear
End If
On Error GoTo 0
%>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Parse_ShouldHandleAspWithSessionAndCookies()
    {
        var sourceCode = @"<%
Session(\"UserName\") = Request.Cookies(\"user\")
If Session(\"UserName\") <> \"\" Then
    Response.Write(\"Welcome back, \" & Session(\"UserName\"))
Else
    Response.Redirect(\"/login.asp\")
End If
%>";
        var result = await _integration.ParseToCognitiveGraphAsync(sourceCode);
        
        Assert.NotNull(result);
    }

    #endregion
}
