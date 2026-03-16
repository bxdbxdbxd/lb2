using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.Text.RegularExpressions;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private string currentFilePath = "";
        private TabControl tabControl;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel lineColumnLabel;
        private ToolStripStatusLabel languageLabel;
        private Dictionary<TabPage, bool> isModified = new Dictionary<TabPage, bool>();
        private bool isUpdatingLineNumbers = false;
        private Dictionary<TabPage, string> filePaths = new Dictionary<TabPage, string>();
        private bool isRussianLanguage = true;
        private Panel outputPanel;
        private DataGridView[] errorGridViews = new DataGridView[5];
        private RichTextBox outputTextBox;
        private TabControl errorTabControl;
        private LexicalAnalyzer lexicalAnalyzer;
        private ToolStripMenuItem runToolStripMenuItem;

        public Form1()
        {
            lexicalAnalyzer = new LexicalAnalyzer();
            InitializeComponent();
            InitializeCustomComponents();
            InitializeKeyboardShortcuts();
        }

        private void InitializeCustomComponents()
        {
            SplitContainer mainVerticalSplit = new SplitContainer();
            mainVerticalSplit.Dock = DockStyle.Fill;
            mainVerticalSplit.Orientation = Orientation.Horizontal;
            mainVerticalSplit.SplitterDistance = this.Height / 2;
            mainVerticalSplit.IsSplitterFixed = false;

            Panel inputPanel = new Panel();
            inputPanel.Dock = DockStyle.Fill;
            CreateTabControl(inputPanel);
            mainVerticalSplit.Panel1.Controls.Add(inputPanel);

            Panel outputPanel = new Panel();
            outputPanel.Dock = DockStyle.Fill;
            outputPanel.BackColor = Color.White;
            outputPanel.BorderStyle = BorderStyle.FixedSingle;

            SplitContainer outputSplitContainer = new SplitContainer();
            outputSplitContainer.Dock = DockStyle.Fill;
            outputSplitContainer.Orientation = Orientation.Vertical;

            Panel errorsPanel = new Panel();
            errorsPanel.Dock = DockStyle.Fill;
            errorsPanel.BackColor = Color.White;
            errorsPanel.BorderStyle = BorderStyle.FixedSingle;

            Label errorsLabel = new Label();
            errorsLabel.Text = isRussianLanguage ? "ТАБЛИЦА ОШИБОК" : "ERRORS TABLE";
            errorsLabel.Dock = DockStyle.Top;
            errorsLabel.TextAlign = ContentAlignment.MiddleCenter;
            errorsLabel.Font = new Font("Arial", 10, FontStyle.Bold);
            errorsLabel.BackColor = Color.LightGray;
            errorsLabel.Height = 25;
            errorsPanel.Controls.Add(errorsLabel);

            errorTabControl = new TabControl();
            errorTabControl.Dock = DockStyle.Fill;

            for (int i = 0; i < 5; i++)
            {
                TabPage errorTab = new TabPage($"Вкладка {i + 1}");

                errorGridViews[i] = new DataGridView();
                errorGridViews[i].Dock = DockStyle.Fill;
                errorGridViews[i].AllowUserToAddRows = false;
                errorGridViews[i].AllowUserToDeleteRows = false;
                errorGridViews[i].ReadOnly = true;
                errorGridViews[i].RowHeadersVisible = true;
                errorGridViews[i].AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                errorGridViews[i].BackgroundColor = Color.White;
                errorGridViews[i].Font = new Font("Consolas", 9);

                errorGridViews[i].Columns.Add("Line", isRussianLanguage ? "Строка" : "Line");
                errorGridViews[i].Columns.Add("Column", isRussianLanguage ? "Колонка" : "Column");
                errorGridViews[i].Columns.Add("Error", isRussianLanguage ? "Ошибка" : "Error");
                errorGridViews[i].Columns.Add("Description", isRussianLanguage ? "Описание" : "Description");
                errorGridViews[i].Columns.Add("Code", isRussianLanguage ? "Код ошибки" : "Error Code");

                errorGridViews[i].Columns["Line"].Width = 60;
                errorGridViews[i].Columns["Column"].Width = 70;
                errorGridViews[i].Columns["Error"].Width = 120;
                errorGridViews[i].Columns["Description"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                errorGridViews[i].Columns["Code"].Width = 90;

                errorTab.Controls.Add(errorGridViews[i]);
                errorTabControl.TabPages.Add(errorTab);
            }

            errorsPanel.Controls.Add(errorTabControl);

            Panel outputTextPanel = new Panel();
            outputTextPanel.Dock = DockStyle.Fill;
            outputTextPanel.BackColor = Color.Black;
            outputTextPanel.BorderStyle = BorderStyle.FixedSingle;

            Label outputLabel = new Label();
            outputLabel.Text = isRussianLanguage ? "ВЫВОД ПРОГРАММЫ" : "PROGRAM OUTPUT";
            outputLabel.Dock = DockStyle.Top;
            outputLabel.TextAlign = ContentAlignment.MiddleCenter;
            outputLabel.Font = new Font("Arial", 10, FontStyle.Bold);
            outputLabel.BackColor = Color.LightGray;
            outputLabel.Height = 25;
            outputTextPanel.Controls.Add(outputLabel);

            outputTextBox = new RichTextBox();
            outputTextBox.Dock = DockStyle.Fill;
            outputTextBox.ReadOnly = true;
            outputTextBox.BackColor = Color.White;
            outputTextBox.ForeColor = Color.Black;
            outputTextBox.Font = new Font("Consolas", 10);
            outputTextBox.WordWrap = true;
            outputTextPanel.Controls.Add(outputTextBox);

            outputSplitContainer.Panel1.Controls.Add(errorsPanel);
            outputSplitContainer.Panel2.Controls.Add(outputTextPanel);

            outputSplitContainer.Resize += (s, e) =>
            {
                if (outputSplitContainer.Width > 0)
                    outputSplitContainer.SplitterDistance = outputSplitContainer.Width / 2;
            };

            if (outputSplitContainer.Width > 0)
                outputSplitContainer.SplitterDistance = outputSplitContainer.Width / 2;
            else
                outputSplitContainer.SplitterDistance = 300;

            outputSplitContainer.IsSplitterFixed = false;

            outputPanel.Controls.Add(outputSplitContainer);
            mainVerticalSplit.Panel2.Controls.Add(outputPanel);

            tableLayoutPanel1.Controls.Clear();
            tableLayoutPanel1.Controls.Add(mainVerticalSplit, 0, 0);

            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Ready");
            lineColumnLabel = new ToolStripStatusLabel("Ln: 1, Col: 1");
            languageLabel = new ToolStripStatusLabel("Language: Russian");

            statusStrip.Items.Add(statusLabel);
            statusStrip.Items.Add(new ToolStripStatusLabel("|"));
            statusStrip.Items.Add(lineColumnLabel);
            statusStrip.Items.Add(new ToolStripStatusLabel("|"));
            statusStrip.Items.Add(languageLabel);

            this.Controls.Add(statusStrip);
            statusStrip.Dock = DockStyle.Bottom;

            this.AllowDrop = true;
            this.DragEnter += Form1_DragEnter;
            this.DragDrop += Form1_DragDrop;

            AddMenuItems();
            this.FormClosing += Form1_FormClosing;

            // УБРАНО создание кнопки Пуск через код

            runToolStripMenuItem = new ToolStripMenuItem();
            runToolStripMenuItem.Text = isRussianLanguage ? "Пуск" : "Run";
            runToolStripMenuItem.ShortcutKeys = Keys.F5;
            runToolStripMenuItem.Click += StartAnalysis_Click;
        }

        private void InitializeKeyboardShortcuts()
        {
            this.KeyPreview = true;

            this.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.N)
                {
                    Click_button_create(s, e);
                    e.Handled = true;
                }
                else if (e.Control && e.KeyCode == Keys.O)
                {
                    Click_button_open(s, e);
                    e.Handled = true;
                }
                else if (e.Control && e.KeyCode == Keys.S)
                {
                    if (e.Shift)
                        Click_button_save_as(s, e);
                    else
                        Click_burron_save(s, e);
                    e.Handled = true;
                }
                else if (e.Control && e.KeyCode == Keys.Z)
                {
                    Undo_Click(s, e);
                    e.Handled = true;
                }
                else if (e.Control && e.KeyCode == Keys.Y)
                {
                    Redo_Click(s, e);
                    e.Handled = true;
                }
                else if (e.Control && e.KeyCode == Keys.X)
                {
                    Cut_Click(s, e);
                    e.Handled = true;
                }
                else if (e.Control && e.KeyCode == Keys.C)
                {
                    Copy_Click(s, e);
                    e.Handled = true;
                }
                else if (e.Control && e.KeyCode == Keys.V)
                {
                    Paste_Click(s, e);
                    e.Handled = true;
                }
                else if (e.Control && e.KeyCode == Keys.A)
                {
                    SelectAll_Click(s, e);
                    e.Handled = true;
                }
                else if (e.Control && e.KeyCode == Keys.F)
                {
                    FindText();
                    e.Handled = true;
                }
                else if (e.Control && e.KeyCode == Keys.H)
                {
                    Help(s, e);
                    e.Handled = true;
                }
                else if (e.Control && e.KeyCode == Keys.W)
                {
                    CloseCurrentTab();
                    e.Handled = true;
                }
                else if (e.Control && e.KeyCode == Keys.Tab)
                {
                    SwitchToNextTab();
                    e.Handled = true;
                }
                else if (e.Control && e.Shift && e.KeyCode == Keys.Tab)
                {
                    SwitchToPreviousTab();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.F5)
                {
                    StartAnalysis_Click(s, e);
                    e.Handled = true;
                }
            };
        }

        private void FindText()
        {
            using (Form findForm = new Form())
            {
                findForm.Text = isRussianLanguage ? "Найти" : "Find";
                findForm.Size = new Size(300, 150);
                findForm.StartPosition = FormStartPosition.CenterParent;

                TextBox findTextBox = new TextBox();
                findTextBox.Location = new Point(10, 10);
                findTextBox.Size = new Size(260, 20);

                Button findButton = new Button();
                findButton.Text = isRussianLanguage ? "Найти" : "Find";
                findButton.Location = new Point(100, 40);
                findButton.DialogResult = DialogResult.OK;

                findForm.Controls.Add(findTextBox);
                findForm.Controls.Add(findButton);

                if (findForm.ShowDialog() == DialogResult.OK)
                {
                    RichTextBox currentTextBox = GetCurrentRichTextBox();
                    if (currentTextBox != null && !string.IsNullOrEmpty(findTextBox.Text))
                    {
                        int startIndex = currentTextBox.SelectionStart + currentTextBox.SelectionLength;
                        int foundIndex = currentTextBox.Text.IndexOf(findTextBox.Text, startIndex, StringComparison.OrdinalIgnoreCase);

                        if (foundIndex >= 0)
                        {
                            currentTextBox.Select(foundIndex, findTextBox.Text.Length);
                            currentTextBox.ScrollToCaret();
                        }
                        else
                        {
                            MessageBox.Show(isRussianLanguage ? "Текст не найден" : "Text not found");
                        }
                    }
                }
            }
        }

        private void CloseCurrentTab()
        {
            if (tabControl.TabPages.Count > 1)
            {
                TabPage currentTab = tabControl.SelectedTab;
                if (CheckSaveBeforeClosing(currentTab))
                {
                    filePaths.Remove(currentTab);
                    isModified.Remove(currentTab);
                    tabControl.TabPages.Remove(currentTab);
                }
            }
        }

        private void SwitchToNextTab()
        {
            if (tabControl.TabPages.Count > 1)
            {
                int nextIndex = (tabControl.SelectedIndex + 1) % tabControl.TabPages.Count;
                tabControl.SelectedIndex = nextIndex;
            }
        }

        private void SwitchToPreviousTab()
        {
            if (tabControl.TabPages.Count > 1)
            {
                int prevIndex = tabControl.SelectedIndex - 1;
                if (prevIndex < 0) prevIndex = tabControl.TabPages.Count - 1;
                tabControl.SelectedIndex = prevIndex;
            }
        }

        private void AddMenuItems()
        {
            ToolStripMenuItem sizeMenuItem = new ToolStripMenuItem("Размер текста");
            sizeMenuItem.DropDownItems.Add("Мелкий (10pt)", null, (s, ev) => ChangeFontSize(10));
            sizeMenuItem.DropDownItems.Add("Средний (12pt)", null, (s, ev) => ChangeFontSize(12));
            sizeMenuItem.DropDownItems.Add("Крупный (14pt)", null, (s, ev) => ChangeFontSize(14));
            sizeMenuItem.DropDownItems.Add("Очень крупный (16pt)", null, (s, ev) => ChangeFontSize(16));
            Edit.DropDownItems.Add(sizeMenuItem);

            ToolStripMenuItem languageMenuItem = new ToolStripMenuItem("Язык");
            languageMenuItem.DropDownItems.Add("Русский", null, (s, ev) => SetRussianLanguage());
            languageMenuItem.DropDownItems.Add("English", null, (s, ev) => SetEnglishLanguage());
            Edit.DropDownItems.Add(languageMenuItem);
        }

        private void CreateTabControl(Panel parentPanel)
        {
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            string tabName = isRussianLanguage ? "Документ 1" : "Document 1";
            TabPage firstTab = new TabPage(tabName);

            CreateTabContent(firstTab);

            tabControl.TabPages.Add(firstTab);
            filePaths[firstTab] = "";
            isModified[firstTab] = false;

            parentPanel.Controls.Add(tabControl);
        }

        private void CreateTabContent(TabPage tabPage)
        {
            Panel textContainer = new Panel();
            textContainer.Dock = DockStyle.Fill;

            Panel tabLineNumbers = new Panel();
            tabLineNumbers.BackColor = Color.FromArgb(240, 240, 240);
            tabLineNumbers.Width = 40;
            tabLineNumbers.Dock = DockStyle.Left;
            tabLineNumbers.Paint += (s, e) => LineNumbersPanel_Paint(s, e, GetRichTextBoxForTab(tabPage));

            CustomRichTextBox richTextBox = new CustomRichTextBox();
            richTextBox.Dock = DockStyle.Fill;
            richTextBox.TextChanged += (s, e) =>
            {
                TextBox1_TextChanged(s, e);
                isModified[tabPage] = true;
                UpdateTabTitle(tabPage);
                HighlightSyntax(richTextBox);
            };
            richTextBox.SelectionChanged += (s, e) =>
            {
                TextBox1_SelectionChanged(s, e);
                UpdateCursorPosition();
            };
            richTextBox.VScroll += (s, e) => tabLineNumbers.Invalidate();
            richTextBox.Font = new Font("Consolas", 12);
            richTextBox.WordWrap = false;

            textContainer.Controls.Add(richTextBox);
            textContainer.Controls.Add(tabLineNumbers);

            tabPage.Controls.Add(textContainer);
        }

        private void HighlightSyntax(CustomRichTextBox richTextBox)
        {
            if (richTextBox == null) return;

            int selectionStart = richTextBox.SelectionStart;
            int selectionLength = richTextBox.SelectionLength;

            string[] keywords = { "if", "else", "for", "while", "do", "switch", "case", "break",
                                 "continue", "return", "int", "float", "double", "char", "string",
                                 "bool", "void", "class", "public", "private", "protected", "static",
                                 "const", "true", "false", "null", "this", "base", "using", "namespace", "let", "parseFloat" };

            string[] types = { "int", "float", "double", "char", "string", "bool", "void", "object" };

            string text = richTextBox.Text;

            richTextBox.SelectAll();
            richTextBox.SelectionColor = Color.Black;

            int commentIndex = text.IndexOf("//");
            while (commentIndex != -1)
            {
                int endLine = text.IndexOf('\n', commentIndex);
                if (endLine == -1) endLine = text.Length;

                richTextBox.Select(commentIndex, endLine - commentIndex);
                richTextBox.SelectionColor = Color.Green;

                commentIndex = text.IndexOf("//", endLine);
            }

            MatchCollection stringMatches = Regex.Matches(text, "\".*?\"");
            foreach (Match match in stringMatches)
            {
                richTextBox.Select(match.Index, match.Length);
                richTextBox.SelectionColor = Color.Brown;
            }

            stringMatches = Regex.Matches(text, "\'.*?\'");
            foreach (Match match in stringMatches)
            {
                richTextBox.Select(match.Index, match.Length);
                richTextBox.SelectionColor = Color.Brown;
            }

            foreach (string keyword in keywords)
            {
                MatchCollection matches = Regex.Matches(text, @"\b" + keyword + @"\b");
                foreach (Match match in matches)
                {
                    richTextBox.Select(match.Index, match.Length);
                    richTextBox.SelectionColor = Color.Blue;
                }
            }

            foreach (string type in types)
            {
                MatchCollection matches = Regex.Matches(text, @"\b" + type + @"\b");
                foreach (Match match in matches)
                {
                    richTextBox.Select(match.Index, match.Length);
                    richTextBox.SelectionColor = Color.Teal;
                }
            }

            MatchCollection numberMatches = Regex.Matches(text, @"\b\d+(?:\.\d+)?(?:e[+-]?\d+)?\b", RegexOptions.IgnoreCase);
            foreach (Match match in numberMatches)
            {
                richTextBox.Select(match.Index, match.Length);
                richTextBox.SelectionColor = Color.Red;
            }

            richTextBox.Select(selectionStart, selectionLength);
            richTextBox.SelectionColor = Color.Black;
        }

        private RichTextBox GetRichTextBoxForTab(TabPage tabPage)
        {
            if (tabPage.Controls.Count > 0)
            {
                Panel container = tabPage.Controls[0] as Panel;
                if (container != null && container.Controls.Count > 0)
                {
                    return container.Controls[0] as RichTextBox;
                }
            }
            return null;
        }

        private void UpdateTabTitle(TabPage tabPage)
        {
            if (isModified.ContainsKey(tabPage) && isModified[tabPage])
            {
                string originalTitle = tabPage.Text;
                if (!originalTitle.EndsWith("*"))
                {
                    tabPage.Text = originalTitle + "*";
                }
            }
            else
            {
                tabPage.Text = tabPage.Text.TrimEnd('*');
            }
        }

        private bool CheckSaveBeforeClosing(TabPage tabPage)
        {
            if (isModified.ContainsKey(tabPage) && isModified[tabPage])
            {
                string fileName = filePaths.ContainsKey(tabPage) && !string.IsNullOrEmpty(filePaths[tabPage])
                    ? Path.GetFileName(filePaths[tabPage])
                    : tabPage.Text.TrimEnd('*');

                string message = isRussianLanguage
                    ? $"Сохранить изменения в файле '{fileName}'?"
                    : $"Save changes to file '{fileName}'?";

                string caption = isRussianLanguage ? "Сохранение" : "Save";

                DialogResult result = MessageBox.Show(message, caption,
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    tabControl.SelectedTab = tabPage;
                    Click_burron_save(null, null);
                    return true;
                }
                else if (result == DialogResult.No)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (TabPage tab in tabControl.TabPages)
            {
                if (!CheckSaveBeforeClosing(tab))
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void LineNumbersPanel_Paint(object sender, PaintEventArgs e, RichTextBox targetTextBox)
        {
            if (isUpdatingLineNumbers) return;

            RichTextBox currentTextBox = targetTextBox ?? GetCurrentRichTextBox();
            if (currentTextBox == null) return;

            isUpdatingLineNumbers = true;

            e.Graphics.Clear(Color.FromArgb(240, 240, 240));

            int firstIndex = currentTextBox.GetCharIndexFromPosition(new Point(0, 0));
            int firstLine = currentTextBox.GetLineFromCharIndex(firstIndex);

            int height = currentTextBox.Height;
            int lineHeight = currentTextBox.Font.Height;
            int visibleLines = height / lineHeight;

            for (int i = 0; i <= visibleLines + 1; i++)
            {
                int lineNumber = firstLine + i + 1;
                if (lineNumber > currentTextBox.Lines.Length) break;

                Point linePos = currentTextBox.GetPositionFromCharIndex(
                    currentTextBox.GetFirstCharIndexFromLine(firstLine + i));

                if (linePos.Y >= 0 && linePos.Y < height)
                {
                    e.Graphics.DrawString(lineNumber.ToString(),
                        currentTextBox.Font,
                        Brushes.Gray,
                        new PointF(5, linePos.Y));
                }
            }

            isUpdatingLineNumbers = false;
        }

        private RichTextBox GetCurrentRichTextBox()
        {
            if (tabControl.SelectedTab != null && tabControl.SelectedTab.Controls.Count > 0)
            {
                Panel container = tabControl.SelectedTab.Controls[0] as Panel;
                if (container != null && container.Controls.Count > 0)
                {
                    return container.Controls[0] as RichTextBox;
                }
            }
            return null;
        }

        private string GetCurrentFilePath()
        {
            if (tabControl.SelectedTab != null && filePaths.ContainsKey(tabControl.SelectedTab))
            {
                return filePaths[tabControl.SelectedTab];
            }
            return "";
        }

        private void SetCurrentFilePath(string path)
        {
            if (tabControl.SelectedTab != null)
            {
                filePaths[tabControl.SelectedTab] = path;
                if (!string.IsNullOrEmpty(path))
                {
                    tabControl.SelectedTab.Text = Path.GetFileName(path);
                    isModified[tabControl.SelectedTab] = false;
                }
            }
        }

        private void TextBox1_Scroll(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null)
            {
                Panel container = tabControl.SelectedTab.Controls[0] as Panel;
                if (container != null && container.Controls.Count > 1)
                {
                    container.Controls[1].Invalidate();
                }
            }
        }

        private void TextBox1_SelectionChanged(object sender, EventArgs e)
        {
            UpdateCursorPosition();
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null)
            {
                Panel container = tabControl.SelectedTab.Controls[0] as Panel;
                if (container != null && container.Controls.Count > 1)
                {
                    container.Controls[1].Invalidate();
                }
            }
        }

        private void UpdateCursorPosition()
        {
            RichTextBox currentTextBox = GetCurrentRichTextBox();
            if (currentTextBox != null)
            {
                int index = currentTextBox.SelectionStart;
                int line = currentTextBox.GetLineFromCharIndex(index);
                int column = index - currentTextBox.GetFirstCharIndexFromLine(line);

                if (isRussianLanguage)
                {
                    lineColumnLabel.Text = $"Стр: {line + 1}, Стб: {column + 1}";
                }
                else
                {
                    lineColumnLabel.Text = $"Ln: {line + 1}, Col: {column + 1}";
                }

                int charCount = currentTextBox.TextLength;
                int lineCount = currentTextBox.Lines.Length;
                string currentPath = GetCurrentFilePath();

                if (string.IsNullOrEmpty(currentPath))
                {
                    if (isRussianLanguage)
                    {
                        statusLabel.Text = $"Новый документ | Символов: {charCount} | Строк: {lineCount}";
                    }
                    else
                    {
                        statusLabel.Text = $"New document | Characters: {charCount} | Lines: {lineCount}";
                    }
                }
                else
                {
                    FileInfo fileInfo = new FileInfo(currentPath);
                    if (isRussianLanguage)
                    {
                        statusLabel.Text = $"Файл: {Path.GetFileName(currentPath)} | Размер: {fileInfo.Length} байт | Символов: {charCount}";
                    }
                    else
                    {
                        statusLabel.Text = $"File: {Path.GetFileName(currentPath)} | Size: {fileInfo.Length} bytes | Characters: {charCount}";
                    }
                }
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                OpenFileInNewTab(files[0]);
            }
        }

        private void OpenFileInNewTab(string filePath)
        {
            try
            {
                string fileContent = System.IO.File.ReadAllText(filePath);

                string tabName = isRussianLanguage ? Path.GetFileName(filePath) : Path.GetFileName(filePath);
                TabPage tabPage = new TabPage(tabName);

                CreateTabContent(tabPage);

                RichTextBox richTextBox = GetRichTextBoxForTab(tabPage);
                if (richTextBox != null)
                {
                    richTextBox.Text = fileContent;
                    HighlightSyntax(richTextBox as CustomRichTextBox);
                }

                tabControl.TabPages.Add(tabPage);
                tabControl.SelectedTab = tabPage;

                filePaths[tabPage] = filePath;
                isModified[tabPage] = false;
            }
            catch (Exception ex)
            {
                string errorMessage = isRussianLanguage ? "Ошибка при открытии файла: " : "Error opening file: ";
                string errorTitle = isRussianLanguage ? "Ошибка" : "Error";

                MessageBox.Show($"{errorMessage}{ex.Message}",
                    errorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateCursorPosition();
            currentFilePath = GetCurrentFilePath();
        }

        private void ChangeFontSize(float newSize)
        {
            RichTextBox currentTextBox = GetCurrentRichTextBox();
            if (currentTextBox != null)
            {
                currentTextBox.Font = new Font("Consolas", newSize);
            }
        }

        private void SetRussianLanguage()
        {
            isRussianLanguage = true;

            File.Text = "Файл";
            Create.Text = "Создать";
            Open.Text = "Открыть";
            Save.Text = "Сохранить";
            Save_as.Text = "Сохранить как";
            Exit.Text = "Выход";

            Edit.Text = "Правка";
            Undo.Text = "Отменить";
            Redo.Text = "Повторить";
            Cut.Text = "Вырезать";
            Copy.Text = "Копировать";
            Paste.Text = "Вставить";
            Delete.Text = "Удалить";
            Select_all.Text = "Выделить всё";

            foreach (ToolStripMenuItem item in Edit.DropDownItems)
            {
                if (item.Text == "Text size" || item.Text == "Размер текста")
                {
                    item.Text = "Размер текста";
                    foreach (ToolStripMenuItem subItem in item.DropDownItems)
                    {
                        if (subItem.Text.Contains("Small")) subItem.Text = "Мелкий (10pt)";
                        else if (subItem.Text.Contains("Medium")) subItem.Text = "Средний (12pt)";
                        else if (subItem.Text.Contains("Large")) subItem.Text = "Крупный (14pt)";
                        else if (subItem.Text.Contains("Extra Large")) subItem.Text = "Очень крупный (16pt)";
                    }
                }
                if (item.Text == "Language" || item.Text == "Язык")
                {
                    item.Text = "Язык";
                }
            }

            Help_me.Text = "Справка";
            Call_help.Text = "Вызов справки";
            About.Text = "О программе";

            toolStripButton1.Text = "Создать";
            toolStripButton2.Text = "Открыть";
            toolStripButton3.Text = "Сохранить";
            toolStripButton4.Text = "Отменить";
            toolStripButton5.Text = "Повторить";
            toolStripButton6.Text = "Копировать";
            toolStripButton7.Text = "Вырезать";
            toolStripButton8.Text = "Вставить";

            if (runToolStripMenuItem != null)
                runToolStripMenuItem.Text = "Пуск";

            languageLabel.Text = "Язык: Русский";
            statusLabel.Text = "Готов к работе";

            UpdateTabTitles();
            UpdateCursorPosition();
        }

        private void SetEnglishLanguage()
        {
            isRussianLanguage = false;

            File.Text = "File";
            Create.Text = "New";
            Open.Text = "Open";
            Save.Text = "Save";
            Save_as.Text = "Save As";
            Exit.Text = "Exit";

            Edit.Text = "Edit";
            Undo.Text = "Undo";
            Redo.Text = "Redo";
            Cut.Text = "Cut";
            Copy.Text = "Copy";
            Paste.Text = "Paste";
            Delete.Text = "Delete";
            Select_all.Text = "Select All";

            foreach (ToolStripMenuItem item in Edit.DropDownItems)
            {
                if (item.Text == "Размер текста" || item.Text == "Text size")
                {
                    item.Text = "Text size";
                    foreach (ToolStripMenuItem subItem in item.DropDownItems)
                    {
                        if (subItem.Text.Contains("Мелкий")) subItem.Text = "Small (10pt)";
                        else if (subItem.Text.Contains("Средний")) subItem.Text = "Medium (12pt)";
                        else if (subItem.Text.Contains("Крупный")) subItem.Text = "Large (14pt)";
                        else if (subItem.Text.Contains("Очень крупный")) subItem.Text = "Extra Large (16pt)";
                    }
                }
                if (item.Text == "Язык" || item.Text == "Language")
                {
                    item.Text = "Language";
                }
            }

            Help_me.Text = "Help";
            Call_help.Text = "Call help";
            About.Text = "About";

            toolStripButton1.Text = "New";
            toolStripButton2.Text = "Open";
            toolStripButton3.Text = "Save";
            toolStripButton4.Text = "Undo";
            toolStripButton5.Text = "Redo";
            toolStripButton6.Text = "Copy";
            toolStripButton7.Text = "Cut";
            toolStripButton8.Text = "Paste";

            if (runToolStripMenuItem != null)
                runToolStripMenuItem.Text = "Run";

            languageLabel.Text = "Language: English";
            statusLabel.Text = "Ready";

            UpdateTabTitles();
            UpdateCursorPosition();
        }

        private void UpdateTabTitles()
        {
            for (int i = 0; i < tabControl.TabPages.Count; i++)
            {
                TabPage tab = tabControl.TabPages[i];
                string baseTitle;

                if (filePaths.ContainsKey(tab) && !string.IsNullOrEmpty(filePaths[tab]))
                {
                    baseTitle = Path.GetFileName(filePaths[tab]);
                }
                else
                {
                    if (isRussianLanguage)
                    {
                        baseTitle = $"Документ {i + 1}";
                    }
                    else
                    {
                        baseTitle = $"Document {i + 1}";
                    }
                }

                tab.Text = isModified.ContainsKey(tab) && isModified[tab] ? baseTitle + "*" : baseTitle;
            }
        }

        private void StartAnalysis_Click(object sender, EventArgs e)
        {
            RichTextBox currentTextBox = GetCurrentRichTextBox();
            if (currentTextBox == null) return;

            string text = currentTextBox.Text;

            for (int i = 0; i < 5; i++)
            {
                errorGridViews[i].Rows.Clear();
                errorGridViews[i].Columns.Clear();
            }
            outputTextBox.Clear();

            outputTextBox.SelectionColor = Color.Black;
            outputTextBox.AppendText($"[{DateTime.Now:T}] Запуск лексического анализа...\n");
            outputTextBox.AppendText($"Анализируемый текст ({text.Length} символов, {currentTextBox.Lines.Length} строк):\n");
            outputTextBox.AppendText(new string('-', 50) + "\n");

            var result = lexicalAnalyzer.Analyze(text);

            DisplayAnalysisResults(result);
        }

        private void DisplayAnalysisResults(LexicalAnalysisResult result)
        {
            for (int i = 0; i < 5; i++)
            {
                errorGridViews[i].Rows.Clear();
                errorGridViews[i].Columns.Clear();
            }

            errorGridViews[0].Columns.Add("Code", "Код");
            errorGridViews[0].Columns.Add("Type", "Тип лексемы");
            errorGridViews[0].Columns.Add("Lexeme", "Лексема");
            errorGridViews[0].Columns.Add("Location", "Позиция");

            errorGridViews[0].Columns["Code"].Width = 50;
            errorGridViews[0].Columns["Type"].Width = 120;
            errorGridViews[0].Columns["Lexeme"].Width = 150;
            errorGridViews[0].Columns["Location"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            foreach (var token in result.Tokens)
            {
                int rowIndex = errorGridViews[0].Rows.Add(
                    token.Code,
                    token.Type,
                    token.Lexeme,
                    token.Location
                );
                errorGridViews[0].Rows[rowIndex].Tag = token;
            }

            errorGridViews[1].Columns.Add("Severity", "Тип");
            errorGridViews[1].Columns.Add("Code", "Код");
            errorGridViews[1].Columns.Add("Line", "Строка");
            errorGridViews[1].Columns.Add("Position", "Позиция");
            errorGridViews[1].Columns.Add("Message", "Сообщение");
            errorGridViews[1].Columns.Add("Context", "Контекст");

            errorGridViews[1].Columns["Severity"].Width = 80;
            errorGridViews[1].Columns["Code"].Width = 70;
            errorGridViews[1].Columns["Line"].Width = 50;
            errorGridViews[1].Columns["Position"].Width = 60;
            errorGridViews[1].Columns["Message"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            errorGridViews[1].Columns["Context"].Width = 150;

            HighlightErrorsInText(result.Errors);

            if (result.Errors.Count > 0)
            {
                outputTextBox.SelectionColor = Color.Black;
                outputTextBox.AppendText($"\n[!] ОБНАРУЖЕНЫ ОШИБКИ: {result.Errors.Count}\n");
                outputTextBox.AppendText("========================================\n");

                foreach (var error in result.Errors)
                {
                    int rowIndex = errorGridViews[1].Rows.Add(
                        error.Severity,
                        error.ErrorCode,
                        error.Line,
                        error.Position,
                        error.Message,
                        error.Context ?? ""
                    );
                    errorGridViews[1].Rows[rowIndex].Tag = error;

                    string severityColor = error.Severity == "КРИТИЧЕСКАЯ" ? "КРАСНЫЙ" :
                                          error.Severity == "ПРЕДУПРЕЖДЕНИЕ" ? "ЖЕЛТЫЙ" : "ОРАНЖЕВЫЙ";

                    outputTextBox.AppendText($"\n[{error.Severity}] {error.ErrorCode}: {error.Message}\n");
                    outputTextBox.AppendText($"  Строка {error.Line}, позиция {error.Position}\n");

                    if (!string.IsNullOrEmpty(error.Character))
                    {
                        outputTextBox.AppendText($"  Недопустимый символ: '{error.Character}'\n");
                    }

                    if (!string.IsNullOrEmpty(error.Context))
                    {
                        outputTextBox.AppendText($"  Контекст: {error.Context}\n");
                        outputTextBox.AppendText($"  {' ',-10}^{new string('~', error.Position - 1)}\n");
                    }
                }

                outputTextBox.AppendText("========================================\n");
            }
            else
            {
                outputTextBox.SelectionColor = Color.Black;
                outputTextBox.AppendText($"\n[?] Ошибок не обнаружено. Все выражения корректны.\n");
            }

            errorGridViews[1].CellClick += ErrorGridView_CellClick;
            errorGridViews[0].CellClick += ErrorGridView_CellClick;

            outputTextBox.SelectionColor = Color.Black;
            outputTextBox.AppendText($"\nСтатистика:\n");
            outputTextBox.AppendText($"- Найдено лексем: {result.Tokens.Count}\n");
            outputTextBox.AppendText($"- Обнаружено ошибок: {result.Errors.Count}\n");

            errorTabControl.SelectedIndex = result.Errors.Count > 0 ? 1 : 0;
        }

        private void HighlightErrorsInText(List<LexicalError> errors)
        {
            RichTextBox currentTextBox = GetCurrentRichTextBox();
            if (currentTextBox == null) return;

            int selectionStart = currentTextBox.SelectionStart;
            int selectionLength = currentTextBox.SelectionLength;

            currentTextBox.SelectAll();
            currentTextBox.SelectionBackColor = Color.White;

            foreach (var error in errors)
            {
                string[] lines = currentTextBox.Lines;
                int charIndex = 0;

                for (int i = 0; i < error.Line - 1; i++)
                {
                    if (i < lines.Length)
                        charIndex += lines[i].Length + 1;
                }

                charIndex += error.Position - 1;

                if (charIndex >= 0 && charIndex < currentTextBox.TextLength)
                {
                    currentTextBox.Select(charIndex, error.Character?.Length ?? 1);

                    if (error.Severity == "КРИТИЧЕСКАЯ")
                        currentTextBox.SelectionBackColor = Color.LightCoral;
                    else if (error.Severity == "ПРЕДУПРЕЖДЕНИЕ")
                        currentTextBox.SelectionBackColor = Color.LightYellow;
                    else
                        currentTextBox.SelectionBackColor = Color.LightSalmon;
                }
            }

            currentTextBox.Select(selectionStart, selectionLength);
            currentTextBox.SelectionBackColor = Color.White;
        }

        private void ErrorGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var grid = sender as DataGridView;
            if (grid?.Rows[e.RowIndex].Tag is LexicalError error)
            {
                RichTextBox currentTextBox = GetCurrentRichTextBox();
                if (currentTextBox != null)
                {
                    string[] lines = currentTextBox.Lines;
                    int charIndex = 0;

                    for (int i = 0; i < error.Line - 1; i++)
                    {
                        if (i < lines.Length)
                            charIndex += lines[i].Length + 1;
                    }

                    charIndex += error.Position - 1;

                    if (charIndex >= 0 && charIndex <= currentTextBox.TextLength)
                    {
                        currentTextBox.SelectionStart = charIndex;
                        currentTextBox.SelectionLength = 1;
                        currentTextBox.ScrollToCaret();
                        currentTextBox.Focus();
                    }
                }
            }
            else if (grid?.Rows[e.RowIndex].Tag is LexicalToken token)
            {
                RichTextBox currentTextBox = GetCurrentRichTextBox();
                if (currentTextBox != null)
                {
                    string[] lines = currentTextBox.Lines;
                    int charIndex = 0;

                    for (int i = 0; i < token.Line - 1; i++)
                    {
                        if (i < lines.Length)
                            charIndex += lines[i].Length + 1;
                    }

                    charIndex += token.StartPosition - 1;
                    int length = token.EndPosition - token.StartPosition + 1;

                    if (charIndex >= 0 && charIndex + length <= currentTextBox.TextLength)
                    {
                        currentTextBox.SelectionStart = charIndex;
                        currentTextBox.SelectionLength = length;
                        currentTextBox.ScrollToCaret();
                        currentTextBox.Focus();
                    }
                }
            }
        }

        private void Click_button_open(object sender, EventArgs e)
        {
            string title = isRussianLanguage ? "Открыть файл" : "Open file";
            string filter = isRussianLanguage ? "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*" : "Text files (*.txt)|*.txt|All files (*.*)|*.*";

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = title;
                openFileDialog.Filter = filter;
                openFileDialog.FilterIndex = 1;
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    OpenFileInNewTab(openFileDialog.FileName);
                }
            }
        }

        private void Click_button_create(object sender, EventArgs e)
        {
            int tabNumber = tabControl.TabPages.Count + 1;
            string tabName = isRussianLanguage ? $"Документ {tabNumber}" : $"Document {tabNumber}";
            TabPage tabPage = new TabPage(tabName);

            CreateTabContent(tabPage);

            tabControl.TabPages.Add(tabPage);
            tabControl.SelectedTab = tabPage;

            filePaths[tabPage] = "";
            isModified[tabPage] = false;
        }

        private void Click_burron_save(object sender, EventArgs e)
        {
            RichTextBox currentTextBox = GetCurrentRichTextBox();
            if (currentTextBox == null) return;

            string currentPath = GetCurrentFilePath();

            if (string.IsNullOrEmpty(currentPath))
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    string filter = isRussianLanguage ? "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*" : "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                    saveFileDialog.Filter = filter;
                    saveFileDialog.DefaultExt = "txt";
                    saveFileDialog.FileName = "MyFile.txt";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        currentPath = saveFileDialog.FileName;
                        SetCurrentFilePath(currentPath);
                    }
                    else
                    {
                        return;
                    }
                }
            }

            try
            {
                System.IO.File.WriteAllText(currentPath, currentTextBox.Text);
                isModified[tabControl.SelectedTab] = false;
                UpdateTabTitle(tabControl.SelectedTab);

                outputTextBox.AppendText($"[{DateTime.Now:T}] Файл сохранен: {currentPath}\n");
            }
            catch (Exception ex)
            {
                string errorMessage = isRussianLanguage ? "Ошибка при сохранении файла: " : "Error saving file: ";
                string errorTitle = isRussianLanguage ? "Ошибка" : "Error";
                MessageBox.Show($"{errorMessage}{ex.Message}",
                    errorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Click_button_save_as(object sender, EventArgs e)
        {
            RichTextBox currentTextBox = GetCurrentRichTextBox();
            if (currentTextBox == null) return;

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                string title = isRussianLanguage ? "Сохранить файл как" : "Save file as";
                string filter = isRussianLanguage ? "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*" : "Text files (*.txt)|*.txt|All files (*.*)|*.*";

                saveFileDialog.Title = title;
                saveFileDialog.Filter = filter;
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.DefaultExt = "txt";
                saveFileDialog.FileName = "MyFile.txt";
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string filePath = saveFileDialog.FileName;
                        string content = currentTextBox.Text;

                        System.IO.File.WriteAllText(filePath, content);
                        SetCurrentFilePath(filePath);
                        isModified[tabControl.SelectedTab] = false;
                        UpdateTabTitle(tabControl.SelectedTab);
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = isRussianLanguage ? "Ошибка: " : "Error: ";
                        string errorTitle = isRussianLanguage ? "Ошибка" : "Error";
                        MessageBox.Show($"{errorMessage}{ex.Message}", errorTitle,
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void Help(object sender, EventArgs e)
        {
            if (isRussianLanguage)
            {
                MessageBox.Show($"СПРАВКА ПО ПРОГРАММЕ\n" +
                    "МЕНЮ ФАЙЛ\n" +
                    "Создать (Ctrl+N) - создает новый документ\n" +
                    "Открыть (Ctrl+O) - открывает существующий файл\n" +
                    "Сохранить (Ctrl+S) - сохраняет текущий документ\n" +
                    "Сохранить как (Ctrl+Shift+S) - сохраняет документ под новым именем\n" +
                    "Выход - завершает работу программы\n\n" +
                    "МЕНЮ ПРАВКА\n" +
                    "Отменить (Ctrl+Z) - отменяет последнее действие\n" +
                    "Повторить (Ctrl+Y) - повторяет отмененное действие\n" +
                    "Вырезать (Ctrl+X) - вырезает выделенный текст\n" +
                    "Копировать (Ctrl+C) - копирует выделенный текст\n" +
                    "Вставить (Ctrl+V) - вставляет текст из буфера\n" +
                    "Удалить (Del) - удаляет выделенный текст\n" +
                    "Выделить все (Ctrl+A) - выделяет весь текст\n" +
                    "Найти (Ctrl+F) - поиск текста\n" +
                    "Размер текста - изменение размера шрифта\n" +
                    "Язык - переключение языка интерфейса\n\n" +
                    "МЕНЮ ПУСК\n" +
                    "Пуск (F5) - запуск лексического анализа\n\n" +
                    "МЕНЮ СПРАВКА\n" +
                    "Вызов справки (Ctrl+H) - открывает это окно\n" +
                    "О программе - информация о программе\n\n" +
                    "ПАНЕЛЬ ИНСТРУМЕНТОВ\n" +
                    "Содержит кнопки быстрого доступа к основным функциям.\n\n" +
                    "ДОПОЛНИТЕЛЬНЫЕ ВОЗМОЖНОСТИ\n" +
                    "Вкладки (Ctrl+Tab, Ctrl+Shift+Tab) - работа с несколькими документами\n" +
                    "Закрыть вкладку (Ctrl+W) - закрывает текущую вкладку\n" +
                    "Нумерация строк - отображается слева от текста\n" +
                    "Подсветка синтаксиса - автоматическая подсветка кода\n" +
                    "Строка состояния - информация о текущем документе\n" +
                    "Drag-and-Drop - перетаскивание файлов в окно",
                    "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"PROGRAM HELP\n" +
                    "FILE MENU\n" +
                    "New (Ctrl+N) - creates a new document\n" +
                    "Open (Ctrl+O) - opens an existing file\n" +
                    "Save (Ctrl+S) - saves the current document\n" +
                    "Save As (Ctrl+Shift+S) - saves the document with a new name\n" +
                    "Exit - exits the program\n\n" +
                    "EDIT MENU\n" +
                    "Undo (Ctrl+Z) - undoes the last action\n" +
                    "Redo (Ctrl+Y) - redoes the undone action\n" +
                    "Cut (Ctrl+X) - cuts the selected text\n" +
                    "Copy (Ctrl+C) - copies the selected text\n" +
                    "Paste (Ctrl+V) - pastes text from the clipboard\n" +
                    "Delete (Del) - deletes the selected text\n" +
                    "Select All (Ctrl+A) - selects all text\n" +
                    "Find (Ctrl+F) - search text\n" +
                    "Text size - changes the font size\n" +
                    "Language - switches the interface language\n\n" +
                    "RUN MENU\n" +
                    "Run (F5) - start lexical analysis\n\n" +
                    "HELP MENU\n" +
                    "Call help (Ctrl+H) - opens this window\n" +
                    "About - information about the program\n\n" +
                    "TOOLBAR\n" +
                    "Contains quick access buttons for main functions.\n\n" +
                    "ADDITIONAL FEATURES\n" +
                    "Tabs (Ctrl+Tab, Ctrl+Shift+Tab) - work with multiple documents\n" +
                    "Close tab (Ctrl+W) - closes the current tab\n" +
                    "Line numbering - displayed to the left of the text\n" +
                    "Syntax highlighting - automatic code highlighting\n" +
                    "Status bar - information about the current document\n" +
                    "Drag-and-Drop - drag files into the window",
                    "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void About_proga(object sender, EventArgs e)
        {
            if (isRussianLanguage)
            {
                MessageBox.Show($"Простой текстовый редактор с функциями языкового процессора.\n" +
                          "Разработан в рамках лабораторной работы по дисциплине\n" +
                          "'Языки и методы программирования'.\n" +
                          "Выполнила Попова Дарья АВТ-314\n\n",
                    "О программе", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Simple text editor with language processor functions.\n" +
                          "Developed as part of laboratory work in the discipline\n" +
                          "'Languages and Programming Methods'.\n" +
                          "Created by Popova Daria AVT-314\n\n",
                    "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void Undo_Click(object sender, EventArgs e)
        {
            RichTextBox currentTextBox = GetCurrentRichTextBox();
            if (currentTextBox != null && currentTextBox.CanUndo)
            {
                currentTextBox.Undo();
            }
        }

        private void Redo_Click(object sender, EventArgs e)
        {
            RichTextBox currentTextBox = GetCurrentRichTextBox();
            if (currentTextBox != null && currentTextBox.CanRedo)
            {
                currentTextBox.Redo();
            }
        }

        private void Cut_Click(object sender, EventArgs e)
        {
            RichTextBox currentTextBox = GetCurrentRichTextBox();
            if (currentTextBox != null && currentTextBox.SelectedText.Length > 0)
            {
                currentTextBox.Cut();
            }
        }

        private void Copy_Click(object sender, EventArgs e)
        {
            RichTextBox currentTextBox = GetCurrentRichTextBox();
            if (currentTextBox != null && currentTextBox.SelectedText.Length > 0)
            {
                currentTextBox.Copy();
            }
        }

        private void Paste_Click(object sender, EventArgs e)
        {
            RichTextBox currentTextBox = GetCurrentRichTextBox();
            if (currentTextBox != null && Clipboard.ContainsText())
            {
                currentTextBox.Paste();
            }
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            RichTextBox currentTextBox = GetCurrentRichTextBox();
            if (currentTextBox != null && currentTextBox.SelectedText.Length > 0)
            {
                int selectionStart = currentTextBox.SelectionStart;
                int selectionLength = currentTextBox.SelectionLength;

                currentTextBox.Text = currentTextBox.Text.Remove(selectionStart, selectionLength);
                currentTextBox.SelectionStart = selectionStart;
            }
        }

        private void SelectAll_Click(object sender, EventArgs e)
        {
            RichTextBox currentTextBox = GetCurrentRichTextBox();
            if (currentTextBox != null)
            {
                currentTextBox.SelectAll();
            }
        }

        private void Button_exit(object sender, EventArgs e)
        {
            this.Close();
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartAnalysis_Click(sender, e);
        }
    }

    public class CustomRichTextBox : RichTextBox
    {
        public CustomRichTextBox()
        {
            this.DetectUrls = false;
            this.WordWrap = false;
        }
    }
}