#!/usr/bin/env dotnet-script

#r "nuget: System.Diagnostics.Process, 4.3.0"
#r "nuget: System.IO.Abstractions, 13.2.11"
#r "nuget: CommandLineParser, 2.9.1"

using CommandLine;
using System;
using System.IO;
using System.Linq;
using System.IO.Abstractions;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

string repoPath = "/Users/momac/Documents/Git/scg2";
string unityProjectPath = "/Users/momac/Documents/Git/scg2";
string unityPath = "/Applications/Unity/Hub/Editor/2022.1.0b6/Unity.app/Contents/MacOS/Unity";
string logFilePath = Path.Combine(unityProjectPath, "build_log.txt");
string buildOutputPath = Path.Combine(unityProjectPath, "__BUILD");

void UpdateGitRepo()
{
    Console.WriteLine("Start Update Git Repo");
    RunProcess("git", "pull origin main", repoPath);
    Console.WriteLine("End Update Git Repo");
}

async Task<bool> BuildUnityProject()
{
    Console.WriteLine("Start Build Unity Project");
    string arguments = $"-quit -batchmode -projectPath {unityProjectPath} -executeMethod BuildScript.PerformBuild -logFile {logFilePath}";
    
    bool buildSuccess = await RunProcessWithTimeoutAsync(unityPath, arguments, unityProjectPath, TimeSpan.FromMinutes(30));
    
    if (buildSuccess)
    {
        buildSuccess = VerifyBuildOutput();
    }
    
    Console.WriteLine($"Build Unity Project {(buildSuccess ? "Succeeded" : "Failed")}");
    return buildSuccess;
}

bool VerifyBuildOutput()
{
    if (!Directory.Exists(buildOutputPath))
    {
        Console.WriteLine("Build output directory not found.");
        return false;
    }

    string[] buildFiles = Directory.GetFiles(buildOutputPath, "*", SearchOption.AllDirectories);
    if (buildFiles.Length == 0)
    {
        Console.WriteLine("No build output files found.");
        return false;
    }

    // 여기에 추가적인 빌드 결과 검증 로직을 구현할 수 있습니다.
    // 예: 특정 파일의 존재 여부, 파일 크기 확인 등

    Console.WriteLine(buildOutputPath);
    Console.WriteLine(SearchOption.AllDirectories);
    //_AdMob("Unity-iPhone", Global.ADMobIOS, $"{Global.S_UNITY_PROJECT_FPATH}/{Global.UNITY_OUTPUT_PATH}");

    Console.WriteLine($"Build output verified. {buildFiles.Length} files found.");
    return true;
}

void RunProcess(string fileName, string arguments, string workingDirectory)
{
    using var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };

    process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
    process.ErrorDataReceived += (sender, args) => Console.WriteLine($"Error: {args.Data}");

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    process.WaitForExit();
}

async Task<bool> RunProcessWithTimeoutAsync(string fileName, string arguments, string workingDirectory, TimeSpan timeout)
{
    using var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };

    process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
    process.ErrorDataReceived += (sender, args) => Console.WriteLine($"Error: {args.Data}");

    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    if (await Task.WhenAny(process.WaitForExitAsync(), Task.Delay(timeout)) == Task.Delay(timeout))
    {
        process.Kill();
        Console.WriteLine("Build process timed out and was terminated.");
        return false;
    }

    return process.ExitCode == 0;
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

Console.WriteLine("Start");
UpdateGitRepo();
bool buildSuccess = await BuildUnityProject();
Console.WriteLine($"Build process {(buildSuccess ? "succeeded" : "failed")}");