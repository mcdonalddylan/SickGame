// Copyright 2019-2019 Alex Quevillon. All Rights Reserved.

using System.Collections.Generic;
using UnityEngine;
using System.IO;

public enum EUtuLog { Log, Warning, Error };

public struct FUtuLog {
	public string message;
	public EUtuLog logCategory;
}

public class UtuLog {
	private static List<FUtuLog> Logs = new List<FUtuLog>();
	private static int errorCount = 0;
	private static int warningCount = 0;
	private static EUtuLog logState = EUtuLog.Log;
	private static string timestamp;

	public static void PrintIntoLogFile(string message) {
		string path = UtuPluginPaths.pluginFolder_Full_Exports + UtuPluginPaths.slash + timestamp + UtuPluginPaths.slash + "UnityExport.log";
		File.AppendAllText(path, message + "\n");
	}

	public static List<FUtuLog> GetLog() {
		return Logs;
	}

	public static void InitializeNewLog(string newTimestamp) {
		timestamp = newTimestamp;
		string path = UtuPluginPaths.pluginFolder_Full_Exports + UtuPluginPaths.slash + timestamp + UtuPluginPaths.slash + "UnityExport.log";
		UtuLog.Log("Log File Path: \"" + path + "\"");
	}
	public static void ClearLog() {
		errorCount = 0;
		warningCount = 0;
		logState = EUtuLog.Log;
		Logs.Clear();
	}

	public static void Empty() {
		Log("\n");
	}
	public static void Separator() {
		Empty();
		Log("---------------------------------------------------------");
		Empty();
	}
	public static void SemiSeparator(string prefix = "") {
		Log(prefix + "----------------------------");
	}

	public static void GetLogState(out EUtuLog outLogState, out int outWarningCount, out int outErrorCount) {
		outLogState = logState;
		outWarningCount = warningCount;
		outErrorCount = errorCount;
	}

	public static void Log(string message) {
		FUtuLog l = new FUtuLog();
		l.message = message;
		l.logCategory = EUtuLog.Log;
		Logs.Add(l);
		Debug.Log("UTU Log - " + message);
		if (l.logCategory == EUtuLog.Log) {
			PrintIntoLogFile("L    " + l.message);
		}
		else if (l.logCategory == EUtuLog.Warning) {
			PrintIntoLogFile("W    " + l.message);
		}
		else if (l.logCategory == EUtuLog.Error) {
			PrintIntoLogFile("E    " + l.message);
		}
	}

	public static void Warning(string message) {
		FUtuLog l = new FUtuLog();
		l.message = message;
		l.logCategory = EUtuLog.Warning;
		Logs.Add(l);
		warningCount++;
		if (logState == EUtuLog.Log) {
			logState = EUtuLog.Warning;
		}
		Debug.LogWarning("UTU Warning - " + message);
	}

	public static void Error(string message) {
		FUtuLog l = new FUtuLog();
		l.message = message;
		l.logCategory = EUtuLog.Error;
		Logs.Add(l);
		errorCount++;
		if (logState == EUtuLog.Log || logState == EUtuLog.Warning) {
			logState = EUtuLog.Error;
		}
		Debug.LogError("UTU Error - " + message);
	}
}
