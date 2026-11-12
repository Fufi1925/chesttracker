using System.Windows;
using Tesseract;
namespace ChestTracker;
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if(e.Args.Length==1&&e.Args[0]=="--verify-ocr")
        {
            StartupUri=null;
            try{TesseractEnviornment.CustomSearchPath=AppContext.BaseDirectory;using var engine=new TesseractEngine(Path.Combine(AppContext.BaseDirectory,"tessdata"),"deu+eng",EngineMode.LstmOnly);Shutdown(0);}
            catch{Shutdown(1);}
            return;
        }
        base.OnStartup(e);
    }
}
