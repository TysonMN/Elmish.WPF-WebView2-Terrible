module MainApp.Program

open Serilog
open Serilog.Extensions.Logging
open Elmish
open Elmish.WPF
open System
open System.Windows
open Microsoft.Web.WebView2.Wpf;


type Model =
    { 
        MainWindowState:WindowState
        Uri: Uri
    }

type Msg =
    | ExitApp
    | ChangeState of WindowState
    | Reload
    | GoBack
    | GoForward
    | SetUri of Uri
    | CmdException of exn

[<RequireQualifiedAccess>]
type Cmd =
    | Reload
    | GoBack
    | GoForward

let homeUri = Uri "https://www.bing.com"

let init =
    {
        MainWindowState = WindowState.Normal
        Uri = homeUri
    }

let update msg m = 
    match msg with 
    | ChangeState s -> { m with MainWindowState = s }, []
    | ExitApp -> m, []
    | Reload -> m, [Cmd.Reload]
    | GoBack -> m, [Cmd.GoBack]
    | GoForward -> m, [Cmd.GoForward]
    | SetUri uri -> { m with Uri = uri }, []
    | CmdException _ -> m, [] // TODO: consider showing the user an error message

let toCmd (webView: WebView2) = function
    | Cmd.Reload -> Cmd.OfFunc.attempt webView.Reload () CmdException
    | Cmd.GoBack -> Cmd.OfFunc.attempt webView.GoBack () CmdException
    | Cmd.GoForward -> Cmd.OfFunc.attempt webView.GoForward () CmdException

let bindings () : Binding<Model, Msg> list = 
    [
        "ExitApp" |> Binding.cmdParam (fun _ -> Environment.Exit 0; ExitApp)
        "MainWindowState" |> Binding.oneWay (fun m -> m.MainWindowState)
        "ChangeSizeCmd" |> Binding.cmd (fun m -> 
            match m.MainWindowState with 
            | WindowState.Normal -> ChangeState WindowState.Maximized
            | _ -> ChangeState WindowState.Normal)
        "ToMinus" |> Binding.cmd (fun _ -> ChangeState WindowState.Minimized)
        "ReloadCmd" |> Binding.cmd Reload
        "ToHome" |> Binding.cmd (SetUri homeUri)
        "GoBack" |> Binding.cmd GoBack
        "GoForward" |> Binding.cmd GoForward
        "WebViewSource" |> Binding.twoWay((fun m -> m.Uri), SetUri)
    ]

let designVm = ViewModel.designInstance init (bindings ())

let main window webView =
    let logger =
        LoggerConfiguration()
            .MinimumLevel.Override("Elmish.WPF.Update", Events.LogEventLevel.Verbose)
            .MinimumLevel.Override("Elmish.WPF.Bindings", Events.LogEventLevel.Verbose)
            .MinimumLevel.Override("Elmish.WPF.Performance", Events.LogEventLevel.Verbose)
            .WriteTo.Console()
            .CreateLogger()
    
    WpfProgram.mkProgramWithCmdMsg (fun () -> init, []) update bindings (toCmd webView)
    |> WpfProgram.withLogger (new SerilogLoggerFactory(logger))
    |> WpfProgram.startElmishLoop window