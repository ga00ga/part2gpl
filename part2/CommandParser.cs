using System;
using System.Drawing;
using System.Windows.Forms;

namespace AES352
{
    public partial class Form1 : Form
    {
        private CommandParser parser;
        private ColorDialog colorDialog;

        public Form1()
        {
            InitializeComponent();
            parser = new CommandParser(codeTextBox, displayArea);
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            displayArea.Paint += new PaintEventHandler(displayArea_Paint);
            colorDialog = new ColorDialog();
            colorDialog.AnyColor = true; // Allow custom color selection
            colorDialog.FullOpen = true; // Show the full dialog
            commandTextBox.KeyUp += new KeyEventHandler(commandTextBox_KeyUp);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Initialization that occurs when the form loads can be placed here.
        }

        private void RunProgramsConcurrently(string program1, string program2)
        {
            var thread1 = new Thread(() => parser.ExecuteProgram(program1));
            var thread2 = new Thread(() => parser.ExecuteProgram(program2));

            thread1.Start();
            thread2.Start();
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            try
            {
                string program = codeTextBox.Text;
                if (string.IsNullOrWhiteSpace(program))
                {
                    MessageBox.Show("No commands to execute.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                parser.ExecuteProgram(program);
                MessageBox.Show("Program executed successfully.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error executing the program: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void SyntaxButton_Click(object sender, EventArgs e)
        {
            try
            {
                parser.CheckSyntax();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Syntax error: " + ex.Message, "Syntax Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text Files (*.txt)|*.txt";
                saveFileDialog.DefaultExt = "txt";
                saveFileDialog.AddExtension = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    parser.SaveProgram(saveFileDialog.FileName);
                }
            }
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Text Files (*.txt)|*.txt";
                openFileDialog.DefaultExt = "txt";
                openFileDialog.AddExtension = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    parser.LoadProgram(openFileDialog.FileName);
                }
            }
        }


        private void commandTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string commandText = commandTextBox.Text.Trim();
                if (string.IsNullOrEmpty(commandText))
                {
                    MessageBox.Show("Please enter a command.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    try
                    {
                        parser.ExecuteCommand(commandText);
                        displayArea.Invalidate(); // Refresh the canvas after executing the command
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    // Removed the clear command to keep the text in the box
                }
                // Prevent the event from bubbling up to other key event handlers
                e.SuppressKeyPress = true;
            }
        }




        private void displayArea_Paint(object sender, PaintEventArgs e)
        {
            // Redraw the image
            if (displayArea.Image != null)
            {
                e.Graphics.DrawImage(displayArea.Image, new Point(0, 0));
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // Check for keyboard shortcuts
            if (e.Control) // Check if the Ctrl key is pressed
            {
                switch (e.KeyCode)
                {
                    case Keys.N: 
                        parser.ExecuteCommand("clear");
                        displayArea.Invalidate(); // Refresh the canvas
                        break;
                    case Keys.O: 
                        using (OpenFileDialog openFileDialog = new OpenFileDialog())
                        {
                            openFileDialog.Filter = "Text Files (*.txt)|*.txt";
                            openFileDialog.DefaultExt = "txt";
                            openFileDialog.AddExtension = true;

                            if (openFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                parser.LoadProgram(openFileDialog.FileName);
                                displayArea.Invalidate(); // Refresh the canvas
                            }
                        }
                        break;
                    case Keys.S: 
                        using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                        {
                            saveFileDialog.Filter = "Text Files (*.txt)|*.txt";
                            saveFileDialog.DefaultExt = "txt";
                            saveFileDialog.AddExtension = true;

                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                parser.SaveProgram(saveFileDialog.FileName);
                            }
                        }
                        break;
                        parser.ExecuteCommand("clear");
                        displayArea.Invalidate(); // Refresh the canvas
                        break;
                }
            }
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            parser.Cleanup();
        }
    }
}
