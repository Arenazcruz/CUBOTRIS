using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Cubotris.Gameplay
{
    [Serializable]
    public sealed class ScoreRecord
    {
        public string id;
        public string playerName;
        public int score;
        public double survivalSeconds;
        public string finishedAtUtc;
        public ScoreRecord Copy() => (ScoreRecord)MemberwiseClone();
    }

    /// <summary>Local, transactional storage. PlayerPrefs is read only when creating the history.</summary>
    public sealed class ScoreHistoryStore
    {
        [Serializable]
        private sealed class Document
        {
            public int version = 1;
            public int historicalUnattributedScore;
            public List<ScoreRecord> records = new List<ScoreRecord>();
        }
        private Document data;
        private bool loadBlocked;
        public string FilePath { get; }
        public string Notice { get; private set; }
        public int HistoricalUnattributedScore => data.historicalUnattributedScore;
        public IReadOnlyList<ScoreRecord> Records => data.records.OrderByDescending(r => r.score)
            .ThenByDescending(r => r.survivalSeconds).ThenBy(r => DateTimeOffset.Parse(r.finishedAtUtc, CultureInfo.InvariantCulture))
            .ThenBy(r => r.id, StringComparer.Ordinal).Select(r => r.Copy()).ToArray();
        public ScoreRecord Best => Records.FirstOrDefault();
        public bool Contains(string id) => data.records.Any(r => r.id == id);

        public ScoreHistoryStore(string path, int previousBest = 0)
        {
            FilePath = path;
            data = new Document { historicalUnattributedScore = Math.Max(0, previousBest) };
            try
            {
                if (File.Exists(path))
                {
                    try { data = Read(path); return; }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("Historial dañado: " + ex.Message);
                        // Keep the original bytes for recovery; never silently overwrite a damaged history.
                        File.Move(path, path + ".corrupt-" + Guid.NewGuid().ToString("N"));
                        Notice = "El historial estaba dañado. Se conservó una copia para recuperarlo.";
                    }
                }
                if (File.Exists(path + ".bak"))
                {
                    try { data = Read(path + ".bak"); Notice = "Se recuperó el historial desde la copia de seguridad."; }
                    catch (Exception ex) { Debug.LogWarning("Copia del historial dañada: " + ex.Message); }
                }
                if (!Commit(data, out string error)) Notice = error;
            }
            catch (Exception ex) { loadBlocked = true; Debug.LogError("No se pudo abrir el historial: " + ex.Message); Notice = "No se pudo abrir el historial. Comprueba los permisos de la carpeta."; }
        }

        public static bool ValidateName(string input, out string name, out string error)
        {
            name = (input ?? "").Trim(); error = null;
            if (name.Length == 0) error = "Escribe tu nombre para guardar el récord.";
            else if (name.Length > 20) error = "El nombre admite un máximo de 20 caracteres.";
            else if ((input ?? "").Any(c => char.IsControl(c)) || name.Any(c => !char.IsLetterOrDigit(c) && c != ' '))
                error = "Usa solamente letras, números y espacios, sin saltos de línea.";
            return error == null;
        }

        public bool TryAdd(ScoreRecord finishedRun, string input, out string error)
        {
            if (!ValidateName(input, out string name, out error)) return false;
            if (finishedRun == null || !Guid.TryParse(finishedRun.id, out _) || finishedRun.score < 0
                || double.IsNaN(finishedRun.survivalSeconds) || double.IsInfinity(finishedRun.survivalSeconds)
                || finishedRun.survivalSeconds < 0 || !ValidDate(finishedRun.finishedAtUtc))
            { error = "Los datos de la partida no son válidos."; return false; }
            if (Contains(finishedRun.id)) { error = "Esta partida ya está guardada."; return false; }
            var next = Clone(); var record = finishedRun.Copy(); record.playerName = name; next.records.Add(record);
            return Commit(next, out error);
        }

        public bool TryDelete(string id, out string error)
        {
            if (!Contains(id)) { error = "El registro seleccionado ya no existe."; return false; }
            var next = Clone(); next.records.RemoveAll(r => r.id == id); return Commit(next, out error);
        }

        public bool TryClear(out string error)
        { var next = Clone(); next.records.Clear(); return Commit(next, out error); }

        private Document Clone() => new Document { historicalUnattributedScore = data.historicalUnattributedScore,
            records = data.records.Select(r => r.Copy()).ToList() };

        private bool Commit(Document next, out string error)
        {
            error = null;
            if (loadBlocked) { error = Notice; return false; }
            string temporary = FilePath + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath)));
                byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(next, true));
                using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                if (File.Exists(FilePath)) File.Replace(temporary, FilePath, FilePath + ".bak");
                else File.Move(temporary, FilePath);
                data = next; return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("No se pudo guardar el historial: " + ex.Message);
                error = "No se pudo guardar el cambio. Comprueba el espacio y los permisos; tus datos anteriores se conservan.";
                return false;
            }
        }

        private static bool ValidDate(string value) => DateTimeOffset.TryParseExact(value, "O", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _);

        private static Document Read(string path)
        {
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json) || !json.Contains("\"version\"") || !json.Contains("\"records\"")) throw new InvalidDataException("Archivo vacío o incompleto");
            var doc = JsonUtility.FromJson<Document>(json);
            if (doc == null || doc.version != 1 || doc.records == null || doc.historicalUnattributedScore < 0) throw new InvalidDataException("Formato incompatible");
            var ids = new HashSet<string>();
            foreach (var record in doc.records)
                if (record == null || !Guid.TryParse(record.id, out _) || !ids.Add(record.id)
                    || !ValidateName(record.playerName, out _, out _) || record.score < 0 || record.survivalSeconds < 0
                    || double.IsNaN(record.survivalSeconds) || double.IsInfinity(record.survivalSeconds) || !ValidDate(record.finishedAtUtc))
                    throw new InvalidDataException("Registro inválido o duplicado");
            return doc;
        }
    }
}
