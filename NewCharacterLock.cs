using BepInEx;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using Steamworks;

[BepInPlugin("com.altair.valheim.newcharacterlock", "New Character Lock", "1.0.0")]
public class NewCharacterLock : BaseUnityPlugin
{
    public static readonly string filePath = Path.Combine(BepInEx.Paths.ConfigPath, "CharacterSteamIDs.txt");
    public static BepInEx.Logging.ManualLogSource PluginLogger;

    private void Awake()
    {
        PluginLogger = Logger;
        if (!SteamManager.Initialized)
        {
            PluginLogger.LogError("Steam API no inicializado.");
            return;
        }

        var harmony = new Harmony("com.altair.valheim.newcharacterlock");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        PluginLogger.LogInfo("Nuevo personaje Lock mod ha sido cargado!");
        EnsureFileExists(filePath);
    }

    private void EnsureFileExists(string path)
    {
        if (!File.Exists(path))
        {
            File.Create(path).Close();
            PluginLogger.LogInfo($"Archivo creado en: {path}");
        }
    }

    public static void Main(string[] args)
    {
        // Punto de entrada de la aplicación
        // Inicializar tu aplicación aquí
        // Por ejemplo, puedes iniciar la lógica del mod aquí
        NewCharacterLock mod = new NewCharacterLock();
        mod.Awake();
    }
}

[HarmonyPatch(typeof(PlayerProfile), "Load")]
public class CharacterLoadPatch
{
    static bool Prefix(PlayerProfile __instance)
    {
        if (!SteamManager.Initialized)
        {
            NewCharacterLock.PluginLogger.LogError("Steam no está inicializado.");
            return false;
        }

        ulong steamID = SteamUser.GetSteamID().m_SteamID;
        string characterName = __instance.GetName();
        string[] lines = File.ReadAllLines(NewCharacterLock.filePath);
        bool steamIDFound = false;
        bool characterRegistered = false;

        foreach (string line in lines)
        {
            string[] parts = line.Split(':');
            if (parts[1] == steamID.ToString())
            {
                steamIDFound = true;
                if (parts[0] == characterName)
                {
                    characterRegistered = true;
                    break;
                }
            }
        }

        if (!steamIDFound)
        {
            // Registra el nuevo personaje ya que no se encontró el SteamID
            File.AppendAllText(NewCharacterLock.filePath, $"{characterName}:{steamID}\n");
            NewCharacterLock.PluginLogger.LogInfo($"Nuevo personaje {characterName} registrado con SteamID: {steamID}");
            return true;
        }
        else if (steamIDFound && !characterRegistered)
        {
            // SteamID existe pero el personaje no
            NewCharacterLock.PluginLogger.LogError($"Acceso denegado para {characterName} con el ID {steamID}: el personaje no está registrado.");
            return false;
        }

        // El SteamID y el personaje ya están registrados
        NewCharacterLock.PluginLogger.LogInfo($"Acceso concedido para {characterName} con el ID {steamID}");
        return true;
    }
}
