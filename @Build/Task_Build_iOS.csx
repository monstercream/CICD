#r "nuget: System.IO.Abstractions, 13.2.11"
#r "nuget: CommandLineParser, 2.9.1"
#load "Utils/Global.csx"
#load "Utils/LogTailer.csx"
#load "Utils/StopWatcher.csx"
#load "Utils/AppCenterUploader.csx"

using CommandLine;
using System;
using System.IO;
using System.Linq;
using System.IO.Abstractions;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

public class Task_Build_iOS
{

    [Verb("build_ios", HelpText = "build ios process.")]
    public class Option_Build_iOS
    {
        [Option('p', "ProjectFPath", Required = false, HelpText = "Set Project Path.")]
        public string ProjectFPath { get; set; }

        [Option('o', "OutputFPath", Required = false, HelpText = "Set Output Path")]
        public string OutputFPath { get; set; }

        [Option('d', "dist", Required = false, HelpText = "Dist", Default = Global.E_RELEASE_TYPE.DEV)]
        public Global.E_RELEASE_TYPE Dist { get; set; }

        [Option('b', "buildnumber", Required = false, HelpText = "Is Upload", Default = 0)]
        public int BuildNumber { get; set; }

        [Option('u', "upload", Required = false, HelpText = "Is Upload", Default = false)]
        public bool IsUpload { get; set; }
        [Option('t', "testflight", Required = false, HelpText = "Is TestFlight", Default = false)]
        public bool IsTestFlight { get; set; }
    }

    public static async Task Run_Option_Build_iOS(Option_Build_iOS opt)
    {
        Global.Init(Global.E_BUILD_TARGET.IOS, opt.Dist, opt.ProjectFPath, opt.OutputFPath);
        new Task_Build_iOS().Build(opt.BuildNumber);

        if (opt.IsUpload)
        {
            if (!File.Exists(Global.BUILD_IPA_FPATH))
            {
                Console.Error.WriteLine($"file not exists: {Global.BUILD_IPA_FPATH}");
                Environment.Exit(1);
            }
            await new AppCenterUploader().Upload_ios(opt.Dist, Global.BUILD_IPA_FPATH);
        }
        if (opt.IsTestFlight)
        {
            _TestFlight(Global.BUILD_IPA_FPATH);
        }
    }

    public void Build(int buildNumber)
    {
        _Unity_XcodeProj(buildNumber);
        _AdMob("Unity-iPhone", Global.ADMobIOS, $"{Global.S_UNITY_PROJECT_FPATH}/{Global.UNITY_OUTPUT_PATH}");
        _Xcode_Archive();
        _Xcode_Ipa();
    }

    void _Unity_XcodeProj(int buildNumber)
    {
        string UNITY_FPATH = Global.UNITY_FPATH;
        string projectPath = Global.S_UNITY_PROJECT_FPATH;
        string outputPath = Global.UNITY_OUTPUT_PATH;
        string dist = Global.S_RELEASE_TYPE.ToString();
        string logFile = Global.UNITY_LOG_FPATH;
        string executeMethod = Global.EXECUTE_METHOD_BUILD;

        Console.WriteLine(UNITY_FPATH);

        File.WriteAllText($"{projectPath}/Assets/Resources/__buildmachine_buildnumber.txt", $"{buildNumber} - {Global.GIT_CURRENT_REVISION}");

        if (Global.S_RELEASE_TYPE == Global.E_RELEASE_TYPE.LIVE)
        {
            File.Delete($"{projectPath}/Assets/Resources/ReportingSettings.asset");
            File.Delete($"{projectPath}/Assets/Resources/ReportingSettings.asset.meta");
        }

        Process process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = UNITY_FPATH,
                Arguments = $"-quit -batchmode -buildTarget iOS -projectPath \"{projectPath}\" -logFile {logFile} -executeMethod {executeMethod} --output {outputPath} --dist {dist} --buildnumber {buildNumber}",
                RedirectStandardOutput = false,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        Console.WriteLine($"{process.StartInfo.FileName} {process.StartInfo.Arguments}");
        using (var _ = new StopWatcher($"Build iOS _Unity_XcodeProj({Global.S_RELEASE_TYPE})"))
        using (LogTailer logTailer = new LogTailer(Global.UNITY_LOG_FPATH))
        {
            process.Start();
            process.WaitForExit();
            int exitCode = process.ExitCode;
            Console.WriteLine($"Done| exitCode:{exitCode}");
            Thread.Sleep(1000 * 5);
        }
    }


    void RunCommand(string command, string arguments, string workingDirectory = "")
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true, // Ensure StandardError is redirected
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? "" : workingDirectory
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd(); // Read StandardError
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Command '{command} {arguments}' failed with exit code {process.ExitCode}\nOutput: {output}\nError: {error}");
        }
        else
        {
            Console.WriteLine(output);
        }
    }

    private void _AdMob(string targetName, string adUnitId, string projectDir)
    {
        Console.WriteLine($"projectDir: {projectDir}");
        var fs = new FileSystem();
        var podfilePath = Path.Combine(projectDir, "Podfile");

        try
        {
            RunCommand("pod", "--version", projectDir);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to check CocoaPods version: {ex.Message}");
            try
            {
                Console.WriteLine($"Installing CocoaPods...");
                RunCommand("sudo", "gem install cocoapods", projectDir);
            }
            catch (Exception installEx)
            {
                Console.WriteLine($"Failed to install CocoaPods: {installEx.Message}");
                Environment.Exit(1);
            }
        }

        if (!fs.File.Exists(podfilePath))
        {
            try
            {
                Console.WriteLine($"Initializing Podfile...");
                RunCommand("pod", "init", projectDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize Podfile: {ex.Message}");
                Environment.Exit(1);
            }
        }

        // Ensure the directory for AppDelegate.swift exists
        var appDelegateDir = Path.Combine(projectDir, targetName);
        if (!fs.Directory.Exists(appDelegateDir))
        {
            fs.Directory.CreateDirectory(appDelegateDir);
        }

        var appDelegatePath = Path.Combine(appDelegateDir, "AppDelegate.swift");
        if (!fs.File.Exists(appDelegatePath))
        {
            // Create the AppDelegate.swift file with basic content
            string appDelegateContent = @"
import UIKit
import GoogleMobileAds

@UIApplicationMain
class AppDelegate: UIResponder, UIApplicationDelegate {

    var window: UIWindow?

    func application(_ application: UIApplication, didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?) -> Bool {
        // Initialize Google Mobile Ads SDK
        GADMobileAds.sharedInstance().start(completionHandler: nil)
        return true
    }
}
";
            fs.File.WriteAllText(appDelegatePath, appDelegateContent);
        }

        var viewControllerPath = Path.Combine(appDelegateDir, "ViewController.swift");
        if (!fs.File.Exists(viewControllerPath))
        {
            // Create the ViewController.swift file with basic content
            string viewControllerContent = $@"
import UIKit
import GoogleMobileAds

class ViewController: UIViewController {{
    var bannerView: GADBannerView!

    override func viewDidLoad() {{
        super.viewDidLoad()

        // AdMob banner ad initialization
        bannerView = GADBannerView(adSize: kGADAdSizeBanner)
        bannerView.adUnitID = ""{adUnitId}""
        bannerView.rootViewController = self
        bannerView.load(GADRequest())
        bannerView.frame = CGRect(x: 0, y: view.frame.size.height - bannerView.frame.size.height, width: bannerView.frame.size.width, height: bannerView.frame.size.height);
        view.addSubview(bannerView);
    }}
}}
";
            fs.File.WriteAllText(viewControllerPath, viewControllerContent);
        }

        try
        {
            Console.WriteLine($"Installing pods...");
            RunCommand("pod", "install", projectDir);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to install pods: {ex.Message}");
            Environment.Exit(1);
        }


        try
        {
            Console.WriteLine($"Update pods...");
            RunCommand("pod", "update", projectDir);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to Update pods: {ex.Message}");
            Environment.Exit(1);
        }

        Console.WriteLine("Google AdMob has been successfully configured. Open the .xcworkspace file in Xcode.");
    }
    void _Xcode_Archive()
    {
        string xcodeproj = $"{Global.S_UNITY_OUTPUT_FPATH}/Unity-iPhone.xcworkspace";
        string archivePath = Global.XCODE_ARCHIEVE_FPATH;
        string derivedDataPath = Global.XCODE_DERIVEDDATE_FPATH;

        Process process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "xcodebuild",
                Arguments = $"-workspace {xcodeproj} -scheme Unity-iPhone -destination \"generic/platform=iOS\" archive -archivePath {archivePath} -derivedDataPath {derivedDataPath} ENABLE_BITCODE=NO OTHER_LDFLAGS=\"$(inherited) -ObjC\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        Console.WriteLine($"{process.StartInfo.FileName} {process.StartInfo.Arguments}");
        using (var _ = new StopWatcher($"Build iOS _Xcode_Archive({Global.S_RELEASE_TYPE}) => {archivePath}"))
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            int exitCode = process.ExitCode;
            Console.WriteLine($"Done| exitCode:{exitCode}\nOutput: {output}\nError: {error}");

            if (exitCode != 0)
            {
                throw new Exception($"xcodebuild archive failed with exit code {exitCode}\nOutput: {output}\nError: {error}");
            }
        }
    }

    void _Xcode_Ipa()
    {
        string exportOptionsPlist = Global.XCODE_EXPORT_OPTION_PLIST_FPATH;
        string archivePath = Global.XCODE_ARCHIEVE_FPATH;
        string exportPath = Global.XCODE_EXPORT_IPA_FPATH;

        if (!Directory.Exists(archivePath))
        {
            throw new Exception($"Archive path does not exist: {archivePath}");
        }

        Process process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "xcodebuild",
                Arguments = $"-exportArchive -archivePath {archivePath} -exportOptionsPlist {exportOptionsPlist} -exportPath {exportPath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        Console.WriteLine($"{process.StartInfo.FileName} {process.StartInfo.Arguments}");
        using (var _ = new StopWatcher($"Build iOS _Xcode_Ipa({Global.S_RELEASE_TYPE})"))
        {
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            int exitCode = process.ExitCode;
            Console.WriteLine($"Done| exitCode:{exitCode}\nOutput: {output}\nError: {error}");

            if (exitCode != 0)
            {
                throw new Exception($"xcodebuild exportArchive failed with exit code {exitCode}\nOutput: {output}\nError: {error}");
            }
        }

        if (!File.Exists($"{exportPath}/MidnightStreet.ipa"))
        {
            throw new Exception($"IPA file not found: {exportPath}/MidnightStreet.ipa");
        }
    }


    private static void _TestFlight(string ipaFpath)
    {
        string apiIssuer = "69a6de7f-adcc-47e3-e053-5b8c7c11a4d1";
        string apiKey = "K2W9LUXW53";

        Process process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "xcrun",
                Arguments = $"altool --validate-app -f {ipaFpath} -t ios --apiKey {apiKey} --apiIssuer {apiIssuer} --verbose",
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        Console.WriteLine($"{process.StartInfo.FileName} {process.StartInfo.Arguments}");
        using (var _ = new StopWatcher($"TestFlight validate"))
        {
            process.Start();
            process.WaitForExit();
            int exitCode = process.ExitCode;
            Console.WriteLine($"Done| exitCode:{exitCode}");
            if (exitCode != 0)
            {
                Environment.Exit(exitCode);
            }
        }

        Process process2 = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "xcrun",
                Arguments = $"altool --upload-app -f {ipaFpath} -t ios --apiKey {apiKey} --apiIssuer {apiIssuer} --verbose",
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        Console.WriteLine($"{process2.StartInfo.FileName} {process2.StartInfo.Arguments}");
        using (var _ = new StopWatcher($"TestFlight upload-app"))
        {
            process2.Start();
            process2.WaitForExit();
            int exitCode = process2.ExitCode;
            Console.WriteLine($"Done| exitCode:{exitCode}");
            if (exitCode != 0)
            {
                Environment.Exit(exitCode);
            }
        }
    }
}

// Global.Init(Global.E_BUILD_TARGET.IOS, Global.E_RELEASE_TYPE.DEV, null, null);
// new Task_Build_iOS().Build();