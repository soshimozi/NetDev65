// See https://aka.ms/new-console-template for more information

using Dev65.W65XX;
using Dev65.XObj;
using Dev65UI;
using Terminal.Gui;

//As65.AssemblerMain(new[] { "test.asm" });

//var parser = new Parser();

////var stream = new FileStream("test.asm", FileMode.Create);
//Parser.Parse("test.obj");


Application.Init();
//Application.Top.ColorScheme = Colors.Base;
//Application.Top.Add(new MainWindow());

//Application.Run(Application.Top);

var Win = new Window($"CTRL-Q to Close - Dev65 IDE")
{
    X = 0,
    Y = 0,
    Width = Dim.Fill(),
    Height = Dim.Fill(),
    ColorScheme = Colors.Base,
};

//Setup();

//Application.Top.Add(Win);
//Application.Run(Application.Top);

Application.Top.Add(new MainWindow());
Application.Run(Application.Top);

// Before the application exits, reset Terminal.Gui for clean shutdown
Application.Shutdown();

//void Quit()
//{
//    Application.RequestStop();
//}

//void Setup()
//{
//    var fileMenu = new MenuBarItem("_File", new MenuItem[]
//    {
//        new("Open _Project", "Open an existing project", () => { }),
//        new("New _Project", "Create a new project", () => { }),
//        new( "_Close Project", "Close the current project", () => { }),
//        new("_Quit", "", Quit)
//    });

//    var settingsMenu = new MenuBarItem("_Settings", new MenuItem[] { });

//    var menu = new MenuBar(new [] {
//                fileMenu,
//                settingsMenu
//            });
//    Application.Top.Add(menu);


//    // Demonstrate Dim & Pos using percentages - a TextField that is 30% height and 80% wide
//    var textView = new TextView()
//    {
//        X = 0 + 2,
//        Y = 0,
//        Width = Dim.Fill(),
//        Height = Dim.Fill(),
//        ColorScheme = Colors.TopLevel,
//    };
//    textView.Text = File.ReadAllText("test.asm");
//    Win.Add(textView);
//}

