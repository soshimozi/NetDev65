using Dev65.W65XX;
using System.Text.RegularExpressions;
using Terminal.Gui;
using static Dev65.W65XX.As65;
using Attribute = Terminal.Gui.Attribute;

namespace Dev65UI;

public class MainWindow : Window
{
    readonly TabView _tabView;
    private int _numberOfNewTabs = 1;
    private readonly StatusItem _lenStatusItem;
    private readonly ListView _errorList;
    private readonly ListView _warningList;
    private readonly List<string> warnings = new();
    private readonly List<string> errors = new();
    public MainWindow()
    {
        //Width = Dim.Percent(80);
        //Height = Dim.Percent(80);

        var menu = new MenuBar(new MenuBarItem[] {
            new MenuBarItem ("_File", new MenuItem [] {
                new MenuItem ("_New", "", New),
                new MenuItem ("_Open", "", Open),
                new MenuItem ("_Save", "", Save),
                new MenuItem ("Save _As", "", () => SaveAs()),
                new MenuItem ("_Close", "", Close),
                new MenuItem ("_Quit", "", Quit),
            }),
            new MenuBarItem ("_Build", new MenuItem [] {
                new MenuItem ("Build _All", "", BuildAll),
                new MenuItem ("_Compile", "", Compile),
                new MenuItem ("_Save", "", Save),
                new MenuItem ("Save _As", "", () => SaveAs()),
                new MenuItem ("_Close", "", Close),
                new MenuItem ("_Quit", "", Quit),
            })

        });

        _tabView = new TabView()
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(15),
        };

        _tabView.TabClicked += TabView_TabClicked;

        _tabView.Style.ShowBorder = true;
        _tabView.ApplyStyleChanges();

        var frameView = new FrameView($"Output")
        {
            X = 0,
            Y = Pos.Bottom(_tabView),
            Width = Dim.Fill(),
            Height = 14
        };

        var outputTabs = new TabView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        frameView.Add(outputTabs);

        _warningList = new ListView(warnings)
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill(),
            Width = Dim.Fill(),
            ColorScheme = new ColorScheme()
        };

        _errorList = new ListView(errors)
        {
            X = 0,
            Y = 0,
            Height = Dim.Fill(),
            Width = Dim.Fill(),
            ColorScheme = new ColorScheme()
        };


        outputTabs.AddTab(new TabView.Tab("Errors", _errorList), true);
        outputTabs.AddTab(new TabView.Tab("Warnings", _warningList), false);

        _errorList.RowRender += ErrorListOnRowRender;
        _warningList.RowRender += WarningListView_RowRender;

        _lenStatusItem = new StatusItem(Key.CharMask, "Len: ", null);
        var statusBar = new StatusBar(new StatusItem[] {
            new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", Quit),
            new StatusItem(Key.CtrlMask | Key.S, "~^S~ Save", Save),
            new StatusItem(Key.CtrlMask | Key.W, "~^W~ Close", Close),
            _lenStatusItem,
        });

        _tabView.SelectedTabChanged += (s, e) => _lenStatusItem.Title = $"Len:{(e.NewTab?.View?.Text?.Length ?? 0)}";

        Add(menu, _tabView, frameView, statusBar);

        New();
    }

    private void ErrorListOnRowRender(ListViewRowEventArgs obj)
    {
        if (obj.Row == _errorList.SelectedItem)
        {
            obj.RowAttribute = new Attribute(Color.Black, Color.White);
        }
    }

    private void WarningListView_RowRender(ListViewRowEventArgs obj)
    {
        if (obj.Row == _warningList.SelectedItem)
        {
            obj.RowAttribute = new Attribute(Color.Black, Color.White);
        }

        //if (obj.Row % 2 == 0)
        //{
        //    obj.RowAttribute = new Attribute(Color.BrightGreen, Color.Magenta);
        //}
        //else
        //{
        //    obj.RowAttribute = new Attribute(Color.BrightMagenta, Color.Green);
        //}
    }

    private void BuildAll()
    {
        // first save all the files
        foreach (var tab in _tabView.Tabs)
        {
            Save(tab);
        }
    }

    private void Compile()
    {
        // let's do some compilation here
        // assemble all the files
        // check for errors
        // if any errors we will display them in a static window somewhere

        // save the current file
        Save();

        if (_tabView.SelectedTab is OpenedFile file)
        {
            var fileName = file.File?.FullName;
            if (string.IsNullOrEmpty(fileName)) return;

            var assembler = As65.CreateAssembler();
            assembler.AssemblerWarning += (sender, args) =>
            {
                //_warningList.Source
                warnings.Add(args.Message);
            };

            assembler.AssemblerError += (sender, args) =>
            {
                errors.Add(args.Message);
            };

            errors.Clear();
            warnings.Clear();
            assembler.Run(new[] { fileName });
        }

    }

    private void Quit()
    {
        Application.RequestStop();
    }

    private void TabView_TabClicked(object? sender, TabView.TabMouseEventArgs e)
    {
        // we are only interested in right clicks
        if (!e.MouseEvent.Flags.HasFlag(MouseFlags.Button3Clicked))
        {
            return;
        }

        MenuBarItem items;

        if (e.Tab == null)
        {
            items = new MenuBarItem(new MenuItem[] {
                new MenuItem ($"Open", "", Open),
            });

        }
        else
        {
            items = new MenuBarItem(new MenuItem[] {
                new MenuItem ($"Save", "", () => Save(e.Tab)),
                new MenuItem ($"Close", "", () => Close(e.Tab)),
            });
        }


        var contextMenu = new ContextMenu(e.MouseEvent.X + 1, e.MouseEvent.Y + 1, items);

        contextMenu.Show();
        e.MouseEvent.Handled = true;
    }

    private void Close()
    {
        Close(_tabView.SelectedTab);
    }
    private void Close(TabView.Tab tabToClose)
    {
        if (tabToClose is not OpenedFile tab)
        {
            return;
        }

        if (tab.UnsavedChanges)
        {

            var result = MessageBox.Query("Save Changes", $"Save changes to {tab.Text?.ToString()?.TrimEnd('*')}", "Yes", "No", "Cancel");

            switch (result)
            {
                case -1:
                case 2:
                    // user cancelled
                    return;
                case 0:
                    tab.Save();
                    break;
            }
        }

        // close and dispose the tab
        _tabView.RemoveTab(tab);
        tab.View.Dispose();
    }

    private bool SaveAs()
    {
        if (_tabView.SelectedTab is not OpenedFile tab)
        {
            return false;
        }

        var fd = new SaveDialog();
        Application.Run(fd);

        if (string.IsNullOrWhiteSpace(fd.FilePath?.ToString()))
        {
            return false;
        }

        if (fd.FileName == null)
        {
            return false;
        }

        tab.File = new FileInfo(fd.FilePath?.ToString() ?? "");
        tab.Text = fd.FileName.ToString();

        _lenStatusItem.Title = $"Len:{(tab.View?.Text?.Length ?? 0)}";
        tab.Save();

        return true;
    }

    private void Save()
    {
        Save(_tabView.SelectedTab);
    }

    private void Save(TabView.Tab tabToSave)
    {
        if (tabToSave is not OpenedFile tab)
        {
            return;
        }

        if (tab.File == null)
        {
            if(!SaveAs()) return;
        }

        tab.Save();
        _tabView.SetNeedsDisplay();
    }

    private void Open()
    {
        var open = new OpenDialog("Open", "Open a file") { AllowsMultipleSelection = true };

        Application.Run(open);

        if (open.Canceled) return;
        foreach (var path in open.FilePaths)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            Open(File.ReadAllText(path), new FileInfo(path), Path.GetFileName(path));
        }
    }

    private void New()
    {
        Open("", null, $"new {_numberOfNewTabs++}");
    }

    private void Open(string initialText, FileInfo? fileInfo, string tabName)
    {
        var textView = new AssemblerView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Text = initialText,
            ColorScheme = Colors.TopLevel
        };

        const string vrule = "|\n1\n2\n3\n4\n5\n6\n7\n8\n9\n";

        var verticalRuler = new Label("")
        {
            X = 0,
            Y = 0,
            Width = 1,
            Height = Dim.Fill(),
            ColorScheme = Colors.Error
        };

        textView.Init();

        var frame = new FrameView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = Colors.TopLevel
        };

        frame.LayoutComplete += (a) => {
            verticalRuler.Text = vrule.Repeat((int)Math.Ceiling((double)(verticalRuler.Bounds.Height * 2) / (double)vrule.Length))[0..(verticalRuler.Bounds.Height * 2)];
        };

        frame.Add(verticalRuler);
        frame.Add(textView);

        var tab = new OpenedFile(tabName, fileInfo, textView);
        _tabView.AddTab(tab, true);

        // when user makes changes rename tab to indicate unsaved
        textView.KeyUp += (k) => {

            // if current text doesn't match saved text
            var areDiff = tab.UnsavedChanges;

            _lenStatusItem.Title = $"Len:{(tab.View?.Text?.Length ?? 0)}";

            if (areDiff)
            {
                if (!tab.Text?.ToString()?.EndsWith('*') != true) return;
                tab.Text = tab.Text?.ToString() + '*';
                _tabView.SetNeedsDisplay();
            }
            else
            {
                if (tab.Text == null) return;
                var text = tab.Text.ToString();

                if (text?.EndsWith('*') != true) return;
                tab.Text = tab.Text?.ToString()?.TrimEnd('*');
                _tabView.SetNeedsDisplay();
            }

        };
    }

    private class AssemblerView : TextView
    {
        private HashSet<string?> keywords = new HashSet<string?>(StringComparer.CurrentCultureIgnoreCase);
        private Attribute blue;
        private Attribute white;
        private Attribute magenta;

        private As65.TokenScanner tokenizer = new TokenScanner();



        public void Init()
        {
            tokenizer.Init();

            keywords.Add("LDA");
            keywords.Add("LDX");
            keywords.Add(".6502");

            keywords.Add("P6501");
            keywords.Add("P6502");
            keywords.Add("P65C02");
            keywords.Add("P65SC02");
            keywords.Add("P65816");
            keywords.Add("P65832");
            keywords.Add("DBREG");
            keywords.Add("DPAGE");
            keywords.Add("ADDR");
            keywords.Add("BSS");
            keywords.Add("Byte");
            keywords.Add("DByte");
            keywords.Add("Word");
            keywords.Add("LONG");
            keywords.Add("Space");
            keywords.Add("Align");
            keywords.Add("Dcb");
            keywords.Add("CODE");
            keywords.Add("DATA");
            keywords.Add("PAGE0");
            keywords.Add("ORG");
            keywords.Add("ELSE");
            keywords.Add("End");
            keywords.Add("ENDIF");
            keywords.Add("ENDM");
            keywords.Add("ENDR");
            keywords.Add("Equ");
            keywords.Add("EXITM");
            keywords.Add("EXTERN");
            keywords.Add("GLOBAL");
            keywords.Add("IF");
            keywords.Add("IFABS");
            keywords.Add("IFNABS");
            keywords.Add("IFREL");
            keywords.Add("IFNREL");
            keywords.Add("IFDEF");
            keywords.Add("IFNDEF");
            keywords.Add("INCLUDE");
            keywords.Add("APPEND");
            keywords.Add("INSERT");
            keywords.Add("LONGA");
            keywords.Add("LONGI");
            keywords.Add("WIDEA");
            keywords.Add("WIDEI");
            keywords.Add("MACRO");
            keywords.Add("ON");
            keywords.Add("Off");
            keywords.Add("REPEAT");
            keywords.Add("Set");
            keywords.Add("LIST");
            keywords.Add("NOLIST");
            keywords.Add("PAGE");
            keywords.Add("TITLE");
            keywords.Add("ERROR");
            keywords.Add("WARN");

            keywords.Add("A2STR");
            keywords.Add("HSTR");
            keywords.Add("PSTR");

            // Functions
            keywords.Add("STRLEN");
            keywords.Add("HI");
            keywords.Add("LO");
            keywords.Add("BANK");

            // Opcodes & Registers
            keywords.Add("A");
            keywords.Add("ADC");
            keywords.Add("AND");
            keywords.Add("ASL");
            keywords.Add("BBR0");
            keywords.Add("BBR1");
            keywords.Add("BBR2");
            keywords.Add("BBR3");
            keywords.Add("BBR4");
            keywords.Add("BBR5");
            keywords.Add("BBR6");
            keywords.Add("BBR7");
            keywords.Add("BBS0");
            keywords.Add("BBS1");
            keywords.Add("BBS2");
            keywords.Add("BBS3");
            keywords.Add("BBS4");
            keywords.Add("BBS5");
            keywords.Add("BBS6");
            keywords.Add("BBS7");
            keywords.Add("BCC");
            keywords.Add("BCS");
            keywords.Add("BEQ");
            keywords.Add("BIT");
            keywords.Add("BMI");
            keywords.Add("BNE");
            keywords.Add("BPL");
            keywords.Add("BRA");
            keywords.Add("BRK");
            keywords.Add("BRL");
            keywords.Add("BVC");
            keywords.Add("BVS");
            keywords.Add("CLC");
            keywords.Add("CLD");
            keywords.Add("CLI");
            keywords.Add("CLV");
            keywords.Add("CMP");
            keywords.Add("COP");
            keywords.Add("CPX");
            keywords.Add("CPY");
            keywords.Add("DEC");
            keywords.Add("DEX");
            keywords.Add("DEY");
            keywords.Add("EOR");
            keywords.Add("HI");
            keywords.Add("INC");
            keywords.Add("INX");
            keywords.Add("INY");
            keywords.Add("JML");
            keywords.Add("JMP");
            keywords.Add("JSL");
            keywords.Add("JSR");
            keywords.Add("LO");
            keywords.Add("LDA");
            keywords.Add("LDX");
            keywords.Add("LDY");
            keywords.Add("LSR");
            keywords.Add("MVN");
            keywords.Add("MVP");
            keywords.Add("NOP");
            keywords.Add("ORA");
            keywords.Add("PEA");
            keywords.Add("PEI");
            keywords.Add("PER");
            keywords.Add("PHA");
            keywords.Add("PHB");
            keywords.Add("PHD");
            keywords.Add("PHK");
            keywords.Add("PHP");
            keywords.Add("PHX");
            keywords.Add("PHY");
            keywords.Add("PLA");
            keywords.Add("PLB");
            keywords.Add("PLD");
            keywords.Add("PLP");
            keywords.Add("PLX");
            keywords.Add("PLY");
            keywords.Add("REP");
            keywords.Add("RMB0");
            keywords.Add("RMB1");
            keywords.Add("RMB2");
            keywords.Add("RMB3");
            keywords.Add("RMB4");
            keywords.Add("RMB5");
            keywords.Add("RMB6");
            keywords.Add("RMB7");
            keywords.Add("ROL");
            keywords.Add("ROR");
            keywords.Add("RTI");
            keywords.Add("RTL");
            keywords.Add("RTS");
            keywords.Add("S");
            keywords.Add("SBC");
            keywords.Add("SEC");
            keywords.Add("SED");
            keywords.Add("SEI");
            keywords.Add("SEP");
            keywords.Add("SMB0");
            keywords.Add("SMB1");
            keywords.Add("SMB2");
            keywords.Add("SMB3");
            keywords.Add("SMB4");
            keywords.Add("SMB5");
            keywords.Add("SMB6");
            keywords.Add("SMB7");
            keywords.Add("STA");
            keywords.Add("STP");
            keywords.Add("STX");
            keywords.Add("STY");
            keywords.Add("STZ");
            keywords.Add("TAX");
            keywords.Add("TAY");
            keywords.Add("TCD");
            keywords.Add("TCS");
            keywords.Add("TDC");
            keywords.Add("TRB");
            keywords.Add("TSB");
            keywords.Add("TSC");
            keywords.Add("TSX");
            keywords.Add("TXA");
            keywords.Add("TXS");
            keywords.Add("TXY");
            keywords.Add("TYA");
            keywords.Add("TYX");
            keywords.Add("WAI");
            keywords.Add("WDM");
            keywords.Add("XBA");
            keywords.Add("XCE");
            keywords.Add("X");
            keywords.Add("Y");

                keywords.Add("IF");
                keywords.Add("ELSE");
                keywords.Add("ENDIF");
                keywords.Add("REPEAT");
                keywords.Add("UNTIL");
                keywords.Add("FOREVER");
                keywords.Add("WHILE");
                keywords.Add("ENDW");
                keywords.Add("CONT");
                keywords.Add("BREAK");
                keywords.Add("EQ");
                keywords.Add("NE");
                keywords.Add("CC");
                keywords.Add("CS");
                keywords.Add("PL");
                keywords.Add("MI");
                keywords.Add("VC");
                keywords.Add("VS");

                // Expanding jumps
                keywords.Add("JCC");
                keywords.Add("JCS");
                keywords.Add("JEQ");
                keywords.Add("JMI");
                keywords.Add("JNE");
                keywords.Add("JPL");
                keywords.Add("JVC");
                keywords.Add("JVS");
                keywords.Add("JPA");
            


            Autocomplete.AllSuggestions = keywords.ToList();

            magenta = Driver.MakeAttribute(Color.Magenta, Color.Black);
            blue = Driver.MakeAttribute(Color.Cyan, Color.Black);
            white = Driver.MakeAttribute(Color.White, Color.Black);
        }

        protected override void SetNormalColor()
        {
            Driver.SetAttribute(white);
        }

        protected override void SetNormalColor(List<Rune> line, int idx)
        {
            // parse line
            var lineText = new string(line.Select(r => (char)r).ToArray());
            tokenizer.Tokenize(lineText);



            if (IsInStringLiteral(line, idx))
            {
                Driver.SetAttribute(magenta);
            }
            else
            if (IsKeyword(line, idx))
            {
                Driver.SetAttribute(blue);
            }
            else
            {
                Driver.SetAttribute(white);
            }
        }

        private bool IsInStringLiteral(List<Rune> line, int idx)
        {
            var strLine = new string(line.Select(r => (char)r).ToArray());

            foreach (Match m in Regex.Matches(strLine, "'[^']*'"))
            {
                if (idx >= m.Index && idx < m.Index + m.Length)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsKeyword(List<Rune> line, int idx)
        {
            var word = IdxToWord(line, idx);

            if (string.IsNullOrWhiteSpace(word))
            {
                return false;
            }

            return keywords.Contains(word, StringComparer.CurrentCultureIgnoreCase);
        }

        private string? IdxToWord(List<System.Rune> line, int idx)
        {
            string?[] words = Regex.Split(
                new string(line.Select(r => (char)r).ToArray()),
                "\\b");


            var count = 0;
            string? current = null;

            foreach (var word in words)
            {
                current = word;
                count += word?.Length ?? 0;
                if (count > idx)
                {
                    break;
                }
            }

            return current?.Trim();
        }
    }

    private class OpenedFile : TabView.Tab
    {
        public FileInfo? File { get; set; }

        /// <summary>
        /// The text of the tab the last time it was saved
        /// </summary>
        /// <value></value>
        private string? _savedText;

        public bool UnsavedChanges => !string.Equals(_savedText, View.Text.ToString());

        public OpenedFile(string name, FileInfo? file, View control) : base(name, control)
        {
            File = file;
            _savedText = control.Text.ToString();
        }

        internal void Save()
        {
            var newText = View.Text.ToString();

            if(File != null)
                System.IO.File.WriteAllText(File.FullName, newText);

            _savedText = newText;

            Text = Text?.ToString()?.TrimEnd('*');
        }
    }

}