/************************************************
SvnMenu.cs

Copyright (c) 2016 LotosLabo

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
************************************************/

using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

/* コンテキストメニューにSVNのコマンドを追加する機能 */
public class SvnMenu : EditorWindow {

	/// <summary>
	/// 固定テキスト名.
	/// </summary>
    
        // コマンド名.
	private static readonly string SVN_COMMAND = "/opt/local/bin/svn";
	private static readonly string LOG_COMMAND = "log";
	private static readonly string STATUS_COMMAND = "status";
	private static readonly string UPDATE_COMMAND = "update";
	private static readonly string REVERT_COMMAND = "revert";
        private static readonly string ADD_COMMAND = "add";
	private static readonly string COMMIT_COMMAND = "commit";
	private static readonly string COMMIT_MESSAGE_COMMAND = "-m";

        // ウインドウタイトル名.
	private static readonly string COMMIT_WINDOW_TITLE = "Commit";
	private static readonly string COMMIT_LABEL_TITLE = "Commit Message";

        // ボタン名.
	private static readonly string OK_BTN_NAME = "OK";
	private static readonly string CLOSE_BTN_NAME = "Close";

        // エラーメッセージ.
	private static readonly string COMMIT_EMPTY_MESSAGCE = "Please enter the message!";


	/// <summary>
	/// SVNコマンドステータス.
	/// </summary>
	private enum State {
		None = 0,
		Log,
		Status,
		Update,
		Revert,
                Add,
		Commit
	}

	/// <summary>
	/// ステータス情報.
	/// </summary>
	private static State m_State = State.None;	

	/// <summary>
	/// プロジェクトのルートパス.
	/// </summary>
	private static string m_parentPath = string.Empty;

	/// <summary>
	/// 実行ファイルコマンド.
	/// </summary>
	private static string m_commandJoin = string.Empty;

	/// <summary>
	/// ログ.
	/// </summary>
	private static string m_Log = string.Empty;

	/// <summary>
	/// インスタンス.
	/// </summary>
	private static SvnMenu svnMenuInstance;

	/// <summary>
	/// コミットメッセージ.
	/// </summary>
	private static string m_commitMessage = string.Empty;

        /// <summary>
        /// 初期化.
        /// </summary>
	void OnEnable() {
        m_parentPath = string.Empty;
		m_commandJoin = string.Empty;
		m_Log = string.Empty;
                m_commitMessage = string.Empty;
	}

	// ログ情報.
	[MenuItem("Assets/SVNMenu/Log")]
	private static void SVNLog() {
		m_State = State.Log;
                GetSelectFile();
	}
	
	// 状態表示.
	[MenuItem("Assets/SVNMenu/Status")]
	private static void SVNStatus() {
		m_State = State.Status;
		GetSelectFile();
	}

	// 更新処理.
	[MenuItem("Assets/SVNMenu/Update")]
	private static void SVNUpdate() {
		m_State = State.Update;
                GetSelectFile();
	}

	// 前回のコミット時点に戻す.
	[MenuItem("Assets/SVNMenu/Revert")]
	private static void SVNRevert() {
		m_State = State.Revert;
		GetSelectFile();
	}

        // 追加する.
        [MenuItem("Assets/SVNMenu/Add")]
        private static void SVNAdd() {
                m_State = State.Add;
        	GetSelectFile();
        }

	// コミットする.
	[MenuItem("Assets/SVNMenu/Commit")]
	private static void SVNCommit() {
		m_State = State.Commit;
		Window_Create(COMMIT_WINDOW_TITLE);
	}

	// ウインドウの作成.
	private static void Window_Create(string title) {
		GetWindow<SvnMenu>().Close();

	        if (svnMenuInstance != null) {
	            svnMenuInstance.Close();
	        }

		svnMenuInstance = CreateInstance<SvnMenu>();

	        GUIContent titleContent = new GUIContent(title);
	        svnMenuInstance.titleContent = titleContent;

		svnMenuInstance.minSize = new Vector2(500,100);
		svnMenuInstance.maxSize = new Vector2(500,100);

		svnMenuInstance.Show();
	}

	void OnGUI() {

	        EditorGUILayout.Space();
	        EditorGUILayout.Space();

		// スタイルの作成.
		GUIStyle guiStyle = new GUIStyle();

		// テキストカラーに赤色を指定.
		guiStyle.normal.textColor = Color.red;

		switch(m_State) {
			case State.Commit:
				m_commitMessage = EditorGUILayout.TextField(COMMIT_LABEL_TITLE, m_commitMessage);
				break;
		}

		EditorGUILayout.Space();
		EditorGUILayout.Space();

		/******************************
		/             ボタン
		******************************/

		EditorGUILayout.BeginHorizontal();

			EditorGUILayout.Space();

			// CLOSEボタン.
			if(GUILayout.Button(CLOSE_BTN_NAME,GUILayout.Width(200),GUILayout.Height(30))) {
				svnMenuInstance.Close();
			}

			// OKボタン.
			if(GUILayout.Button(OK_BTN_NAME,GUILayout.Width(200),GUILayout.Height(30))) {
				switch(m_State) {
					case State.Commit:
						// コミットメッセージが空の時.
						if(string.IsNullOrEmpty(m_commitMessage)) {
							m_Log = COMMIT_EMPTY_MESSAGCE;
							return;
						}
						break;
				}

				m_Log = string.Empty;
				GetSelectFile();
			}

			EditorGUILayout.Space();

		EditorGUILayout.EndHorizontal();

		// ログ.
		EditorGUILayout.LabelField(m_Log, guiStyle);

	}

	// ファイル選択.
	private static void GetSelectFile() {
		// 選択されている時.
		if(Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0) {
			// リストを作成.
			List<string> filelist = new List<string>();

			// 選択されているファイルを取得.
			foreach(var files in Selection.assetGUIDs) {
				// ファイルのパスを取得.
				var path = AssetDatabase.GUIDToAssetPath(files);
				filelist.Add(path);
			}

			// プロジェクトのルートパス取得.
			m_parentPath = Application.dataPath;

			//　コマンド用リスト.
			List<string> commandList = new List<string>();

			// ファイル名から"Assets"を取り除く.
			for(int index = 0; index < filelist.Count; ++index) {
				string commandPath = filelist[index].Remove(0, filelist[index].IndexOf("/") + 1);
				commandList.Add(commandPath);
			}

			// コマンドを連結して空白を代入.
			m_commandJoin = string.Join(" ", commandList.ToArray());

			Process_Start();
		}
		else {
			m_State = State.None;
		}
	}

	

	// プロセスの開始.
	private static void Process_Start() {

		// プロセス作成.
		System.Diagnostics.Process process = new System.Diagnostics.Process();

		// プロセス起動にシェルを使用するかどうか.
		process.StartInfo.UseShellExecute = false;

		// 入力を可能にする.
		process.StartInfo.RedirectStandardInput = true;

		// 出力を読み取り可能にする.
		process.StartInfo.RedirectStandardOutput = true;

		// プロセス出力イベント設定.
		process.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(OutputHandler);

		// エラー出力読み取り可.
		process.StartInfo.RedirectStandardError = true;

		// エラー出力イベント設定.
		process.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(ErrorOutputHanlder);

		// 作業ディレクトリにプロジェクトのルートパスを指定.
		process.StartInfo.WorkingDirectory = m_parentPath;

		// svnコマンドを実行.
        #if UNITY_EDITOR_WIN
            process.StartInfo.FileName = System.Environment.ExpandEnvironmentVariables("svn");
        #endif

        #if UNITY_EDITOR_OS
            process.StartInfo.FileName = SVN_COMMAND;
        #endif

		// 新しいウインドウを作成しない.
		process.StartInfo.CreateNoWindow = true;

		string command = null;

		// コマンドの指定.
		switch(m_State) {
			case State.None:
				command = null;
				break;
			
			case State.Log:
				command = LOG_COMMAND + " " + m_commandJoin;
				break;

			case State.Status:
				command = STATUS_COMMAND + " " + m_commandJoin;
				break;

			case State.Update:
				command = UPDATE_COMMAND + " " + m_commandJoin;
				break;

			case State.Revert:
				command = REVERT_COMMAND + " " + m_commandJoin;
				break;

		        case State.Add:
		                command = ADD_COMMAND + " " + m_commandJoin;
		                break;

			case State.Commit:
				if(!string.IsNullOrEmpty(m_commitMessage)) {
					command = COMMIT_COMMAND + " " + m_commandJoin + " " + COMMIT_MESSAGE_COMMAND + " " +  m_commitMessage;
				}
				break;
		}

		if(command != null) {
			// コマンドとパスを引数に.
			process.StartInfo.Arguments = command;

			// プロセス終了時にExitedイベントを発生.
			process.EnableRaisingEvents = true;

			// プロセス終了時に呼び出されるイベントの設定.
			process.Exited += new System.EventHandler(Process_Exit);

			// プロセススタート.
			process.Start();

			Debug.Log("---------------------------------- Process Start ----------------------------------");
					
			// プロセス結果出力.
			process.BeginOutputReadLine();
			
			// プロセスエラー結果出力.
	  		process.BeginErrorReadLine();
		}
			
	}

	// 標準出力時.
	private static void OutputHandler(object sender, System.Diagnostics.DataReceivedEventArgs args) {
		string data = args.Data;

		if(!string.IsNullOrEmpty(data)) {
			string encData = EncodeBinaryToString(data);

			if(encData == null) {
				Debug.Log(data);
			} else {
				Debug.Log(encData);
			}
		}

	}
	
	// エラー出力時.
	private static void ErrorOutputHanlder(object sender, System.Diagnostics.DataReceivedEventArgs args) {
		string data = args.Data;

		if(!string.IsNullOrEmpty(data)) {
			string encData = EncodeBinaryToString(data);

			if(encData == null) {
				Debug.Log(data);
			} else {
				Debug.Log(encData);
			}
		}
	}

	// プロセス終了時.
	private static void Process_Exit(object sender, System.EventArgs e) {
		System.Diagnostics.Process proc = (System.Diagnostics.Process)sender;
		Debug.Log("---------------------------------- Process End -----------------------------------");
		svnMenuInstance.Close();
		proc.Kill();
	}

	// 文字化けを日本語文字列に変換する.
	private static string EncodeBinaryToString(string data) {
		// 返すテキスト.
		string encResult = null;

		List<string> hexArray = new List<string>();
		List<string> codeArray = new List<string>();

		// {U+16進}から16進のみを取得するためのマッチパターン作成.
		string getHexPattern = @"(\{U\+)(?<Result>.+?)(\})";

		// {U+16進}を全て取得するためのマッチパターン作成.
		string getcodePattern = @"(\{.*?\})";

		// 正規表現作成.
		System.Text.RegularExpressions.Match hexMatch;
		System.Text.RegularExpressions.Match codeMatch;

		// 文字化け文字を取得.
		codeMatch = System.Text.RegularExpressions.Regex.Match(data, getcodePattern);

		// マッチしている間.
		while(codeMatch.Success) {
			codeArray.Add(codeMatch.Value);

			// 次のマッチングへ.
			codeMatch = codeMatch.NextMatch();
		}

		// マッチしたものがない場合はnullを返す.
		if(!System.Text.RegularExpressions.Regex.IsMatch(data, getcodePattern)) {
			encResult = null;
			return encResult;
		}
		
		// ASCIIコードのみをを取得.
		hexMatch = System.Text.RegularExpressions.Regex.Match(data, getHexPattern);

		while(hexMatch.Success) {
			hexArray.Add(hexMatch.Groups["Result"].Value);
			hexMatch = hexMatch.NextMatch();
		}

		List<string> japaneseCharArray = new List<string>();


		// ASCIIコードを日本語文字列に変換する.
		for(int index = 0; index < hexArray.Count; ++index) {
			// 16進数を基に32bit符号付き変数に変換.
			int intCode16 = Convert.ToInt32(hexArray[index], 16);

			// char型に変換.
			char conChar = Convert.ToChar(intCode16);

			// string文字列に変換.
			string strChar = conChar.ToString();
			japaneseCharArray.Add(strChar);
		}

		// 元の文字列を日本語文字列に置き換える.
		for(int index = 0; index < japaneseCharArray.Count; ++index) {
			data = data.Replace(codeArray[index], japaneseCharArray[index]);
		}
		
		encResult = data;
		return encResult;
		
	}

}
