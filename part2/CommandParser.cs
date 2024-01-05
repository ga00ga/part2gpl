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
    private Dictionary<string, List<string>> subroutines;
    private Dictionary<string, Action<string[]>> methods;
    public PointF currentPosition;
    private bool fillEnabled = false;

    // Make properties public so they can be accessed by unit tests
    public PointF CurrentPosition => currentPosition;
    public Pen CurrentPen => currentPen;
    public bool FillEnabled => fillEnabled;
    private readonly object graphicsLock = new object();

    public CommandParser(TextBox codeTextBox, PictureBox displayArea)
    {
        this.codeTextBox = codeTextBox;
        this.displayArea = displayArea;

        // Setup bitmap to draw on
        Bitmap bmp = new Bitmap(displayArea.Width, displayArea.Height);
        displayArea.Image = bmp;
        this.graphics = Graphics.FromImage(bmp);

        // Initialize currentPen with default color and opacity
        currentPen = new Pen(Color.Black, 1f); // Default opacity: 255 (fully opaque)
        currentPosition = new PointF(0, 0);

        variables = new Dictionary<string, float>();
        subroutines = new Dictionary<string, List<string>>();
        methods = new Dictionary<string, Action<string[]>>();
    }


    public void ExecuteProgram(string program)
    {
        lock (graphicsLock)
        {
            var lines = codeTextBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue; // Ignore empty lines and comments

                var parts = line.Split(' ');
                if (parts[0].ToLower() == "method")
                {
                    string methodName = parts[1];
                    List<string> methodCommands = new List<string>();
                    i++; // Move to the next line to start reading the method body
                    while (!lines[i].Trim().StartsWith("endmethod", StringComparison.OrdinalIgnoreCase))
                    {
                        methodCommands.Add(lines[i]);
                        i++;
                    }
                    // Store the method body in a lambda that takes parameters
                    methods[methodName] = (parameters) =>
                    {
                        foreach (var cmd in methodCommands)
                        {
                            ExecuteCommand(ReplaceParameters(cmd, parameters));
                        }
                    };
                    continue;
                }
                else if (methods.ContainsKey(parts[0]))
                {
                    // Extracting parameters and removing parentheses
                    var parameters = parts[1].Trim('(', ')').Split(',');
                    methods[parts[0]](parameters);
                    continue;
                }

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
                    case "setcolor":
                        SetColor(Color.FromName(parts[1]), int.Parse(parts[2]));
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
                    case "bgcolor":
                        if (parts.Length >= 2)
                        {
                            string colorName = parts[1];
                            ChangeBackgroundColor(colorName);
                            displayArea.Invalidate(); // Refresh the canvas
                        }
                        else
                        {
                            // Handle error: Invalid or missing color name parameter
                            Console.WriteLine("Invalid 'bgcolor' command. Usage: bgcolor [colorName]");
                        }
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

                    case "drawgrid":
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int spacing))
                        {
                            DrawGridlines(spacing);
                        }
                        else
                        {
                            // Handle error: Invalid or missing spacing parameter
                            Console.WriteLine("Invalid 'drawgrid' command. Usage: drawgrid [spacing]");
                        }
                        break;


                    default:
                        throw new ArgumentException("Unknown command");
                }
            }

            // After executing a command that draws something, refresh the PictureBox
            displayArea.Invalidate();
        }
    }



    public void ExecuteCommand(string commandText)
    {
        string[] lines = commandText.Split(' ');
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
            case "setcolor":
                SetColor(Color.FromName(lines[1]), int.Parse(lines[2]));
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

    public void ChangeBackgroundColor(string colorName)
    {
        if (Enum.TryParse(colorName, true, out KnownColor knownColor))
        {
            Color newColor = Color.FromKnownColor(knownColor);
            displayArea.BackColor = newColor; // Change the background color of the drawing area
        }
        else
        {
            throw new ArgumentException("Invalid color name.");
        }
    }


    private float UseVariable(string varName)
    {
        if (variables.TryGetValue(varName, out float value))
        {
            return value;
        }
        throw new ArgumentException($"Variable '{varName}' not found.");
    }
    private int FindEndLoopIndex(string[] lines, int startIndex)
    {
        int loopCount = 0;
        for (int i = startIndex; i < lines.Length; i++)
        {
            var trimmedLine = lines[i].Trim().ToLower();
            if (trimmedLine.StartsWith("loop"))
            {
                loopCount++;
            }
            else if (trimmedLine.StartsWith("endloop"))
            {
                loopCount--;
                if (loopCount < 0)
                {
                    return i; // Found the matching "endloop"
                }
            }
        }
        throw new InvalidOperationException("No matching endloop found for loop at line " + startIndex);
    }

    private int FindEndIfIndex(string[] lines, int startIndex)
    {
        int ifCount = 0;
        for (int i = startIndex; i < lines.Length; i++)
        {
            var trimmedLine = lines[i].Trim().ToLower();
            if (trimmedLine.StartsWith("if"))
            {
                ifCount++;
            }
            else if (trimmedLine.StartsWith("endif"))
            {
                ifCount--;
                if (ifCount < 0)
                {
                    return i; // Found the matching "endif"
                }
            }
        }
        throw new InvalidOperationException("No matching endif found for if at line " + startIndex);
    }



    private bool EvaluateCondition(string condition)
    {
        string[] parts = condition.Split(' ');
        if (parts.Length == 3)
        {
            float left = ParseFloat(parts[0]);
            float right = ParseFloat(parts[2]);

            switch (parts[1])
            {
                case "==": return left == right;
                case "!=": return left != right;
                case "<": return left < right;
                case ">": return left > right;
                case "<=": return left <= right;
                case ">=": return left >= right;
                default: throw new ArgumentException("Invalid operator in condition");
            }
        }
        throw new ArgumentException("Invalid format for condition");
    }

    public void ExecuteSubroutine(string name)
    {
        if (!subroutines.ContainsKey(name))
            throw new ArgumentException($"Subroutine '{name}' not found.");

        foreach (var command in subroutines[name])
        {
            ExecuteCommand(command);
        }
    }

    private string ReplaceParameters(string command, string[] parameters)
    {
        foreach (var param in parameters)
        {
            var parts = param.Split('=');
            if (parts.Length == 2)
            {
                command = command.Replace(parts[0].Trim(), parts[1].Trim());
            }
        }
        return command;
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

    private void DrawGridlines(int spacing)
    {
        using (var gridBitmap = new Bitmap(displayArea.Width, displayArea.Height))
        using (var gridGraphics = Graphics.FromImage(gridBitmap))
        {
            gridGraphics.Clear(Color.White); // Clear the grid with the background color

            using (var gridPen = new Pen(Color.Gray))
            {
                for (int x = 0; x < displayArea.Width; x += spacing)
                {
                    gridGraphics.DrawLine(gridPen, x, 0, x, displayArea.Height);
                }
                for (int y = 0; y < displayArea.Height; y += spacing)
                {
                    gridGraphics.DrawLine(gridPen, 0, y, displayArea.Width, y);
                }
            }

            // Draw the existing image (shapes) on top of the grid
            graphics.DrawImage(gridBitmap, new Point(0, 0));
        }
    }




    private void SetColor(Color color, int opacity)
    {
        // Update the current pen color with the specified opacity
        currentPen.Color = Color.FromArgb(opacity, color);
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
