﻿using xagc_autominer.common;
using xagc_autominer.model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace xagc_autominer.bll
{
    public class Miner
    {
        static Process process = null;

        public enum E_GPUType
        {
            Navida = 0,
            AMD = 1,
        }

        private static string GetMinerString(E_GPUType e_gpuType, string algo, string coin, string poolUrl, string account)
        {
            bool is64Bit = WinOSHelper.get64bit();
            switch (e_gpuType)
            {
                case E_GPUType.Navida:
                    return System.Windows.Forms.Application.StartupPath + (is64Bit ? "\\addon\\nvidia\\win_x64\\z-enemy.exe" : "\\addon\\nvidia\\win_x86\\z-enemy.exe")
                        + " -a " + algo
                        + " -o stratum+tcp://" + poolUrl
                        + " -u " + account
                        + " -p c=" + coin;
                case E_GPUType.AMD:
                    return System.Windows.Forms.Application.StartupPath + "\\addon\\amd\\win_x86\\sgminer.exe"
                        + " -k " + algo
                        + " -o stratum+tcp://" + poolUrl
                        + " -u " + account
                        + " -p c=" + coin
                    + " -I 19";
                default:
                    return "";
            }
        }

        public static bool DoStartMining(bool started, DataReceivedEventHandler OutputHandler)
        {
            if (started)
            {
                DoCloseMiner();
                return false;
            }
            else
            {
                DoCloseMiner();

                Control.CheckForIllegalCrossThreadCalls = false;
                process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.WorkingDirectory = ".";
                process.StartInfo.UseShellExecute = false;    //Whether or not the operating system shell is used to start
                process.StartInfo.RedirectStandardInput = true;//Accepting input information from the calling program
                process.StartInfo.RedirectStandardOutput = true;//Getting the output information by the calling program
                process.StartInfo.RedirectStandardError = true;//Redirection standard error output
                process.StartInfo.CreateNoWindow = true;//Do not display a program window
                process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
                process.Start();//Startup program
                Pool mPool = new PoolBLL().get(Config.configPool);
                switch ((Miner.E_GPUType)Config.configGPUType)
                {
                    case E_GPUType.Navida:
                        break;
                    case E_GPUType.AMD:
                        process.StandardInput.WriteLine("set GPU_FORCE_64BIT_PTR=0");
                        process.StandardInput.WriteLine("set GPU_USE_SYNC_OBJECTS=1");
                        process.StandardInput.WriteLine("set GPU_MAX_ALLOC_PERCENT=100");
                        process.StandardInput.WriteLine("set GPU_SINGLE_ALLOC_PERCENT=100");
                        process.StandardInput.WriteLine("set GPU_MAX_HEAP_SIZE=100");
                        break;
                    default:
                        break;
                }
                process.StandardInput.WriteLine(Miner.GetMinerString((Miner.E_GPUType)Config.configGPUType, "x16r", "XAGC", mPool != null ? mPool.poolStratumUrl : null, Config.configAccount));
                process.BeginOutputReadLine();

                return true;
            }
        }

        public static void DoCloseMiner()
        {
            if (process != null)
            {
                process.Close();
            }

            Process[] allProcess = Process.GetProcesses();
            foreach (Process p in allProcess)
            {
                if (p.ProcessName.ToLower() + ".exe" == "ccminer.exe".ToLower() || p.ProcessName.ToLower() + ".exe" == "ccminer-x64.exe".ToLower() || p.ProcessName.ToLower() + ".exe" == "sgminer.exe".ToLower() || p.ProcessName.ToLower() + ".exe" == "z-enemy.exe".ToLower())
                {
                    for (int i = 0; i < p.Threads.Count; i++)
                        p.Threads[i].Dispose();
                    p.Kill();
                    break;
                }
            }
        }
    }
}
