using System.Collections.Generic;
using UnityEngine;
public class FormatMail { public string id; public List<Bon> rewards; }
public class Bon {}
public class ConfigLoader : MonoBehaviour { public static ConfigLoader Instance; public static System.Action onParseEnded; public List<FormatMail> allMails; }
public class ModelStatistics : MonoBehaviour { public static ModelStatistics instance; public int GetStatValue(string key) => 0; public void SetStatValue(string key, int value) {} }
public class MainStates : MonoBehaviour { public static MainStates instance; public void AddItems(List<Bon> rewards) {} }
