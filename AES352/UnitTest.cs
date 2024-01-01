using NUnit.Framework;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

[TestFixture]
public class CommandParserTests
{
    private CommandParser parser;
    private TextBox codeTextBox;
    private PictureBox displayArea;

    [SetUp]
    public void Setup()
    {
        codeTextBox = new TextBox();
        displayArea = new PictureBox();
        parser = new CommandParser(codeTextBox, displayArea);
    }

    [Test]
    public void TestMoveToCommand()
    {
        parser.ExecuteCommand("moveto 100 100");
        Assert.AreEqual(new PointF(100, 100), parser.CurrentPosition);
    }

    [Test]
    public void TestDrawToCommand()
    {
        parser.ExecuteCommand("drawto 200 200");
        Assert.AreEqual(new PointF(200, 200), parser.CurrentPosition);
    }

    [Test]
    public void TestClearCommand()
    {
        parser.ExecuteCommand("moveto 100 100");
        parser.ExecuteCommand("clear");
        Assert.AreEqual(new PointF(0, 0), parser.CurrentPosition);
    }

    [Test]
    public void TestDrawRectangleCommand()
    {
        parser.ExecuteCommand("rectangle 50 50");
        // Check if the last drawn rectangle size matches
        Assert.AreEqual(new SizeF(50, 50), parser.GetLastDrawnRectangleSize());
    }

    [Test]
    public void TestDrawCircleCommand()
    {
        parser.ExecuteCommand("circle 30");
        // Check if the last drawn circle radius matches
        Assert.AreEqual(30, parser.GetLastDrawnCircleRadius());
    }

    [Test]
    public void TestDrawTriangleCommand()
    {
        parser.ExecuteCommand("triangle 50 50 100 50 75 100");
        // Check if the last drawn triangle points match
        Assert.AreEqual(new PointF(50, 50), parser.GetLastDrawnTrianglePoints()[0]);
        Assert.AreEqual(new PointF(100, 50), parser.GetLastDrawnTrianglePoints()[1]);
        Assert.AreEqual(new PointF(75, 100), parser.GetLastDrawnTrianglePoints()[2]);
    }

    [Test]
    public void TestSetColorCommand()
    {
        parser.ExecuteCommand("color Red");
        Assert.AreEqual(Color.Red, parser.CurrentPen.Color);
    }

    [Test]
    public void TestResetPenPositionCommand()
    {
        parser.ExecuteCommand("moveto 100 100");
        parser.ExecuteCommand("reset");
        Assert.AreEqual(new PointF(0, 0), parser.CurrentPosition);
    }

    [Test]
    public void TestToggleFillCommand()
    {
        parser.ExecuteCommand("fill on");
        Assert.IsTrue(parser.FillEnabled);

        parser.ExecuteCommand("fill off");
        Assert.IsFalse(parser.FillEnabled);
    }

    [Test]
    public void TestSyntaxChecking()
    {
        // Test valid and invalid commands
        Assert.DoesNotThrow(() => parser.CheckSyntax());

        codeTextBox.Text = "invalid_command";
        Assert.Throws<SyntaxErrorException>(() => parser.CheckSyntax());
    }
}
[Test]
public void TestVariableDeclaration()
{
    parser.ExecuteCommand("var x 10");
    Assert.AreEqual(10f, parser.GetVariableValue("x"));
}

[Test]
public void TestVariableAssignment()
{
    parser.ExecuteCommand("var x 10");
    parser.ExecuteCommand("set x 20");
    Assert.AreEqual(20f, parser.GetVariableValue("x"));
}

[Test]
public void TestMethodDefinitionAndCall()
{
    parser.ExecuteCommand("method myMethod");
    parser.ExecuteCommand("moveto 100 100");
    parser.ExecuteCommand("drawto 200 200");
    parser.ExecuteCommand("endmethod");
    parser.ExecuteCommand("myMethod");
    Assert.AreEqual(new PointF(200, 200), parser.CurrentPosition);
}

[Test]
public void TestIfStatement()
{
    parser.ExecuteCommand("var x 10");
    parser.ExecuteCommand("if x > 5");
    parser.ExecuteCommand("moveto 100 100");
    parser.ExecuteCommand("endif");
    Assert.AreEqual(new PointF(100, 100), parser.CurrentPosition);
}

[Test]
public void TestLoop()
{
    parser.ExecuteCommand("loop");
    parser.ExecuteCommand("moveto 100 100");
    parser.ExecuteCommand("drawto 200 200");
    parser.ExecuteCommand("endloop");
    Assert.AreEqual(new PointF(200, 200), parser.CurrentPosition);
}
