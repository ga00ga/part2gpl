using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using static System.Windows.Forms.LinkLabel;

public class CommandParser
{
    private TextBox codeTextBox;
    private PictureBox displayArea;
    private Graphics graphics;
    private Pen currentPen;
    private Dictionary<string, float> variables;
    public PointF currentPosition;
    private bool fillEnabled = false;

    // Make properties public so they can be accessed by unit tests
    public PointF CurrentPosition => currentPosition;
    public Pen CurrentPen => currentPen;
    public bool FillEnabled => fillEnabled;

    public CommandParser(TextBox codeTextBox, PictureBox displayArea)
    {
        this.codeTextBox = codeTextBox;
        this.displayArea = displayArea;

        // Setup bitmap to draw on
        Bitmap bmp = new Bitmap(displayArea.Width, displayArea.Height);
        displayArea.Image = bmp;
        this.graphics = Graphics.FromImage(bmp);
        this.currentPen = new Pen(Color.Black);
        this.currentPosition = new PointF(0, 0);

        variables = new Dictionary<string, float>();
    }

    public void ExecuteProgram(string program)
    {
        var lines = codeTextBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            var parts = line.Split(' ');
            switch (parts[0].ToLower())
            {
                case "moveto":
                    MoveTo(float.Parse(parts[1]), float.Parse(parts[2]));
                    break;
                case "drawto":
                    DrawTo(float.Parse(parts[1]), float.Parse(parts[2]));
                    break;
                case "clear":
                    Clear();
                    break;
                case "rectangle":
                    DrawRectangle(float.Parse(parts[1]), float.Parse(parts[2]));
                    break;
                case "circle":
                    DrawCircle(float.Parse(parts[1]));
                    break;
                case "triangle":
                    DrawTriangle(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]), float.Parse(parts[4]), float.Parse(parts[5]), float.Parse(parts[6]));
                    break;
                case "color":
                    SetColor(Color.FromName(parts[1]));
                    break;
                case "reset":
                    ResetPenPosition();
                    break;
                case "fill":
                    ToggleFill(parts[1]);
                    break;
                case "set":
                    SetVariable(parts[1], ParseFloat(parts[2]));
                    break;
                case "usevar":
                    UseVariable(parts[1]);
                    break;
                case "loop":
                    int iterations = (int)ParseFloat(parts[1]);
                    int endLoopIndex = FindEndLoopIndex(lines, i);
                    for (int j = 0; j < iterations; j++)
                    {
                        for (int k = i + 1; k < endLoopIndex; k++)
                        {
                            ExecuteCommand(lines[k]);
                        }
                    }
                    i = endLoopIndex;
                    break;

                case "if":
                    bool condition = EvaluateCondition(parts[1]);
                    int endIfIndex = FindEndIfIndex(lines, i);
                    if (condition)
                    {
                        for (int k = i + 1; k < endIfIndex; k++)
                        {
                            ExecuteCommand(lines[k]);
                        }
                    }
                    i = endIfIndex;
                    break;
                default:
                    throw new ArgumentException("Unknown command");
            }
        }

        // After executing a command that draws something, refresh the PictureBox
        displayArea.Invalidate();
    }



    public void ExecuteCommand(string command)
    {
        string[] lines = command.Split(' ');
        switch (lines[0].ToLower())
        {
            case "moveto":
                MoveTo(ParseFloat(lines[1]), ParseFloat(lines[2]));
                break;
            case "drawto":
                DrawTo(ParseFloat(lines[1]), ParseFloat(lines[2]));
                break;
            case "clear":
                Clear();
                break;
            case "rectangle":
                DrawRectangle(ParseFloat(lines[1]), ParseFloat(lines[2]));
                break;
            case "circle":
                DrawCircle(ParseFloat(lines[1]));
                break;
            case "triangle":
                DrawTriangle(ParseFloat(lines[1]), ParseFloat(lines[2]), ParseFloat(lines[3]), ParseFloat(lines[4]), ParseFloat(lines[5]), ParseFloat(lines[6]));
                break;
            case "color":
                SetColor(Color.FromName(lines[1]));
                break;
            case "reset":
                ResetPenPosition();
                break;
            case "fill":
                ToggleFill(lines[1]);
                break;
            default:
                throw new ArgumentException($"Unknown command: {lines[0]}");
        }
        displayArea.Invalidate();
    }

    private void SetVariable(string varName, float value)
    {
        variables[varName] = value;
    }

    private float UseVariable(string varName)
    {
        if (variables.TryGetValue(varName, out float value))
        {
            return value;
        }
        throw new ArgumentException($"Variable '{varName}' not found.");
    }
    
    private float ParseFloat(string input)
    {
        if (variables.ContainsKey(input))
        {
            return UseVariable(input);
        }

        if (float.TryParse(input, out float result))
            return result;
        throw new ArgumentException($"Unable to parse '{input}' as a float or variable.");
    }

    public void SaveProgram(string filePath)
    {
        File.WriteAllText(filePath, codeTextBox.Text);
    }

    public void LoadProgram(string filePath)
    {
        codeTextBox.Text = File.ReadAllText(filePath);
    }

    public void CheckSyntax()
    {
        var commands = codeTextBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var command in commands)
        {
            if (!IsValidCommand(command.Trim()))
            {
                MessageBox.Show($"Syntax error in command: {command}", "Syntax Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        MessageBox.Show("All commands have valid syntax.", "Syntax Check", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    private bool IsValidCommand(string command)
    {
        var lines = command.Split(' ');
        var commandType = lines[0].ToLower();

        try
        {
            switch (commandType)
            {
                case "moveto":
                case "drawto":
                    return lines.Length == 3 && lines.Skip(1).All(p => float.TryParse(p, out _));
                case "rectangle":
                    return lines.Length == 5 && lines.Skip(1).All(p => float.TryParse(p, out _));
                case "circle":
                    return lines.Length == 2 && float.TryParse(lines[1], out _);
                case "triangle":
                    return lines.Length == 7 && lines.Skip(1).All(p => float.TryParse(p, out _));
                case "color":
                    return lines.Length == 2 && Enum.IsDefined(typeof(KnownColor), lines[1]);
                case "clear":
                case "reset":
                    return lines.Length == 1;
                case "fill":
                    return lines.Length == 2 && (lines[1].ToLower() == "on" || lines[1].ToLower() == "off");
                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private void MoveTo(float x, float y)
    {
        currentPosition = new PointF(x, y);
    }

    private void DrawTo(float x, float y)
    {
        PointF newPosition = new PointF(x, y);
        graphics.DrawLine(currentPen, currentPosition, newPosition);
        currentPosition = newPosition;
    }

    private void Clear()
    {
        graphics.Clear(Color.White); 
        currentPosition = new PointF(0, 0);
    }

    private void DrawRectangle(float width, float height)
    {
        if (fillEnabled)
            graphics.FillRectangle(currentPen.Brush, currentPosition.X, currentPosition.Y, width, height);
        else
            graphics.DrawRectangle(currentPen, currentPosition.X, currentPosition.Y, width, height);
    }

    private void DrawCircle(float radius)
    {
        if (fillEnabled)
            graphics.FillEllipse(currentPen.Brush, currentPosition.X - radius, currentPosition.Y - radius, radius * 2, radius * 2);
        else
            graphics.DrawEllipse(currentPen, currentPosition.X - radius, currentPosition.Y - radius, radius * 2, radius * 2);
    }

    private void DrawTriangle(float x1, float y1, float x2, float y2, float x3, float y3)
    {
        PointF[] points = { new PointF(x1, y1), new PointF(x2, y2), new PointF(x3, y3) };
        if (fillEnabled)
            graphics.FillPolygon(currentPen.Brush, points);
        else
            graphics.DrawPolygon(currentPen, points);
    }

    private void SetColor(Color color)
    {
        currentPen.Color = color;
    }

    private void ResetPenPosition()
    {
        currentPosition = new PointF(0, 0);
    }

    private void ToggleFill(string state)
    {
        fillEnabled = state.ToLower() == "on";
    }

    public void SetupGraphics(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
    }

    public void Cleanup()
    {
        if (graphics != null)
            graphics.Dispose();
        if (currentPen != null)
            currentPen.Dispose();
    }

}