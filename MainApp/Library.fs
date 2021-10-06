module MainApp.Program
open Serilog
open Serilog.Extensions.Logging
open Elmish.WPF
open System
open System.Windows
open System.Windows.Input
open Microsoft.Web.WebView2.Wpf;

type Model =
    { 
        MainWindowState:WindowState
        MainWebView:WebView2
    }

type Msg =
    | ExitApp
    | ChangeState of WindowState
    | Reload
    | ToHome
    | GoBack
    | GoForward

let init =
    {
        MainWindowState = WindowState.Normal
        MainWebView = 
            let foo = new WebView2(Source = Uri "https://www.bing.com") 
            foo.CoreWebView2InitializationCompleted.Add(fun e -> 
                foo.CoreWebView2.NewWindowRequested.Add(fun e -> 
                    e.Handled <- true
                    foo.Source <- Uri e.Uri))
            foo
    }

let update msg m = 
    match msg with 
    | ChangeState s -> { m with MainWindowState = s }
    | ExitApp -> m
    | Reload -> m.MainWebView.Reload(); m
    | ToHome -> m.MainWebView.Source <- Uri "https://www.bing.com"; m
    | GoBack -> m.MainWebView.GoBack(); m
    | GoForward -> m.MainWebView.GoForward(); m

let bindings () : Binding<Model, Msg> list = 
    [
        "ExitApp" |> Binding.cmdParam (fun obj -> Environment.Exit 0; ExitApp)
        "MainWindowState" |> Binding.oneWay (fun m -> m.MainWindowState)
        "ChangeSizeCmd" |> Binding.cmd (fun m -> 
            match m.MainWindowState with 
            | WindowState.Normal -> ChangeState (WindowState.Maximized)
            | _ -> ChangeState (WindowState.Normal))
        "ToMinus" |> Binding.cmd(fun m -> ChangeState (WindowState.Minimized))
        "MainWebView" |> Binding.oneWay(fun m -> m.MainWebView)
        "ReloadCmd" |> Binding.cmd Reload
        "ToHome" |> Binding.cmd ToHome
        "GoBack" |> Binding.cmd GoBack
        "GoForward" |> Binding.cmd GoForward
    ]

let designVm = ViewModel.designInstance init (bindings ())

let main window =

    let logger =
        LoggerConfiguration()
            .MinimumLevel.Override("Elmish.WPF.Update", Events.LogEventLevel.Verbose)
            .MinimumLevel.Override("Elmish.WPF.Bindings", Events.LogEventLevel.Verbose)
            .MinimumLevel.Override("Elmish.WPF.Performance", Events.LogEventLevel.Verbose)
            .WriteTo.Console()
            .CreateLogger()
    
    WpfProgram.mkSimple (fun () -> init) update bindings
    |> WpfProgram.withLogger (new SerilogLoggerFactory(logger))
    |> WpfProgram.startElmishLoop window