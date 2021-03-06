﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Battleships.Entities.Enums;
using NLog;

namespace Battleships.AiTester.Entities
{
	public class Ai
	{
		private Process process;
		private readonly string exePath;
		private readonly ProcessMonitor monitor;
		private readonly Logger logger;

		public Ai(string exePath, ProcessMonitor monitor)
		{
			this.exePath = exePath;
			this.monitor = monitor;
			logger = StrategyLogManager.GetLogger(Name);
		}

		public string Name
		{
			get { return Path.GetFileNameWithoutExtension(exePath); }
		}

		public TimeSpan TotalProcessorTime
		{
			get
			{
				if (process == null)
					return TimeSpan.FromTicks(0);
				return process.TotalProcessorTime;
			}
		}

		public Vector Init(int width, int height, int[] shipSizes)
		{
			if (process == null || process.HasExited) process = RunProcess();
			SendMessage("Init {0} {1} {2}", width, height, string.Join(" ", shipSizes));
			return ReceiveNextShot();
		}

		public Vector GetNextShot(Vector lastShotTarget, ShotResult lastShot)
		{
			SendMessage("{0} {1} {2}", lastShot, lastShotTarget.X, lastShotTarget.Y);
			return ReceiveNextShot();
		}

		private void SendMessage(string messageFormat, params object[] args)
		{
			var message = string.Format(messageFormat, args);
			logger.Info(message);
			process.StandardInput.WriteLine(message);
		}

		public void Dispose()
		{
			if (process == null || process.HasExited) return;
			process.StandardInput.Close();

			try
			{
				process.Kill();
			}
			catch
			{
				//nothing to do
			}
			process = null;
		}

		private Process RunProcess()
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = exePath,
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			};
			var aiProcess = Process.Start(startInfo);
			monitor.Register(aiProcess);
			return aiProcess;
		}

		private Vector ReceiveNextShot()
		{
			string output;
			try
			{
				output = process.StandardOutput.ReadLine();
				if (output == null)
				{
					var err = process.StandardError.ReadToEnd();
					Console.WriteLine(err);
					logger.Info("No ai output");
					logger.Error(err);
					throw new Exception("No ai output");
				}
			}
			catch (Exception)
			{
				throw;
			}
			try
			{
				var parts = output.Split(' ').Select(int.Parse).ToList();
				var message = string.Format("Hit {0} {1}", parts[0], parts[1]);
				logger.Info(message);
				return new Vector(parts[0], parts[1]);
			}
			catch (Exception e)
			{
				throw new Exception("Wrong ai output: " + output, e);
			}
		}
	}
}
