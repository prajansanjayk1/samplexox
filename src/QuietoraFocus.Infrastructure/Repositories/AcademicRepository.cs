using Microsoft.Data.Sqlite;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Core.Services;
using QuietoraFocus.Infrastructure.Data;

namespace QuietoraFocus.Infrastructure.Repositories;

public sealed class AcademicRepository
{
    private readonly QuietoraDatabase _db;

    public AcademicRepository(QuietoraDatabase db)
    {
        _db = db;
    }

    #region Subjects

    public List<AcademicSubject> GetAllSubjects()
    {
        var list = new List<AcademicSubject>();
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, code, semester, target_hours, completed_minutes, color_hex, notes FROM academic_subjects ORDER BY name ASC;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new AcademicSubject
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Code = reader.GetString(2),
                Semester = reader.GetString(3),
                TargetHours = reader.GetInt32(4),
                CompletedMinutes = reader.GetInt32(5),
                ColorHex = reader.GetString(6),
                Notes = reader.GetString(7)
            });
        }
        reader.Close();

        // Populate topics
        foreach (var sub in list)
        {
            sub.Topics = GetTopicsForSubject(sub.Id, conn);
        }

        return list;
    }

    public long AddSubject(AcademicSubject subject)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO academic_subjects (name, code, semester, target_hours, completed_minutes, color_hex, notes, created_utc)
            VALUES ($name, $code, $sem, $target, $comp, $color, $notes, $now);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$name", subject.Name);
        cmd.Parameters.AddWithValue("$code", subject.Code);
        cmd.Parameters.AddWithValue("$sem", subject.Semester);
        cmd.Parameters.AddWithValue("$target", subject.TargetHours);
        cmd.Parameters.AddWithValue("$comp", subject.CompletedMinutes);
        cmd.Parameters.AddWithValue("$color", subject.ColorHex);
        cmd.Parameters.AddWithValue("$notes", subject.Notes);
        cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O"));

        var id = (long)(cmd.ExecuteScalar() ?? 0L);
        subject.Id = id;
        return id;
    }

    public void IncrementSubjectStudyMinutes(long subjectId, int minutes)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE academic_subjects SET completed_minutes = completed_minutes + $min WHERE id = $id;";
        cmd.Parameters.AddWithValue("$min", minutes);
        cmd.Parameters.AddWithValue("$id", subjectId);
        cmd.ExecuteNonQuery();
    }

    public void DeleteSubject(long subjectId)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM academic_subjects WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", subjectId);
        cmd.ExecuteNonQuery();
    }

    #endregion

    #region Topics

    private List<AcademicTopic> GetTopicsForSubject(long subjectId, SqliteConnection conn)
    {
        var list = new List<AcademicTopic>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, subject_id, title, chapter, estimated_minutes, is_completed FROM academic_topics WHERE subject_id = $id;";
        cmd.Parameters.AddWithValue("$id", subjectId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new AcademicTopic
            {
                Id = r.GetInt64(0),
                SubjectId = r.GetInt64(1),
                Title = r.GetString(2),
                Chapter = r.GetString(3),
                EstimatedMinutes = r.GetInt32(4),
                IsCompleted = r.GetInt32(5) == 1
            });
        }
        return list;
    }

    public void AddTopic(AcademicTopic topic)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO academic_topics (subject_id, title, chapter, estimated_minutes, is_completed)
            VALUES ($sid, $title, $chap, $est, $comp);
            """;
        cmd.Parameters.AddWithValue("$sid", topic.SubjectId);
        cmd.Parameters.AddWithValue("$title", topic.Title);
        cmd.Parameters.AddWithValue("$chap", topic.Chapter);
        cmd.Parameters.AddWithValue("$est", topic.EstimatedMinutes);
        cmd.Parameters.AddWithValue("$comp", topic.IsCompleted ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public void ToggleTopicCompletion(long topicId, bool isCompleted)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE academic_topics SET is_completed = $comp WHERE id = $id;";
        cmd.Parameters.AddWithValue("$comp", isCompleted ? 1 : 0);
        cmd.Parameters.AddWithValue("$id", topicId);
        cmd.ExecuteNonQuery();
    }

    #endregion

    #region Assignments

    public List<AcademicAssignment> GetAllAssignments()
    {
        var list = new List<AcademicAssignment>();
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT a.id, a.subject_id, s.name, a.title, a.description, a.due_date_utc, a.priority, a.status
            FROM academic_assignments a
            LEFT JOIN academic_subjects s ON a.subject_id = s.id
            ORDER BY a.due_date_utc ASC;
            """;
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new AcademicAssignment
            {
                Id = r.GetInt64(0),
                SubjectId = r.GetInt64(1),
                SubjectName = r.IsDBNull(2) ? "General" : r.GetString(2),
                Title = r.GetString(3),
                Description = r.GetString(4),
                DueDateUtc = DateTime.TryParse(r.GetString(5), out var d) ? d : DateTime.UtcNow.AddDays(7),
                Priority = r.GetInt32(6),
                Status = (AssignmentStatus)r.GetInt32(7)
            });
        }
        return list;
    }

    public void AddAssignment(AcademicAssignment assignment)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO academic_assignments (subject_id, title, description, due_date_utc, priority, status)
            VALUES ($sid, $title, $desc, $due, $pri, $stat);
            """;
        cmd.Parameters.AddWithValue("$sid", assignment.SubjectId);
        cmd.Parameters.AddWithValue("$title", assignment.Title);
        cmd.Parameters.AddWithValue("$desc", assignment.Description);
        cmd.Parameters.AddWithValue("$due", assignment.DueDateUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$pri", assignment.Priority);
        cmd.Parameters.AddWithValue("$stat", (int)assignment.Status);
        cmd.ExecuteNonQuery();
    }

    public void UpdateAssignmentStatus(long assignmentId, AssignmentStatus status)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE academic_assignments SET status = $stat WHERE id = $id;";
        cmd.Parameters.AddWithValue("$stat", (int)status);
        cmd.Parameters.AddWithValue("$id", assignmentId);
        cmd.ExecuteNonQuery();
    }

    public void DeleteAssignment(long assignmentId)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM academic_assignments WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", assignmentId);
        cmd.ExecuteNonQuery();
    }

    #endregion

    #region Exams

    public List<AcademicExam> GetAllExams()
    {
        var list = new List<AcademicExam>();
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT e.id, e.subject_id, s.name, e.title, e.exam_date_utc, e.target_score, e.syllabus_completed_percent, e.notes
            FROM academic_exams e
            LEFT JOIN academic_subjects s ON e.subject_id = s.id
            ORDER BY e.exam_date_utc ASC;
            """;
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new AcademicExam
            {
                Id = r.GetInt64(0),
                SubjectId = r.GetInt64(1),
                SubjectName = r.IsDBNull(2) ? "General" : r.GetString(2),
                Title = r.GetString(3),
                ExamDateUtc = DateTime.TryParse(r.GetString(4), out var d) ? d : DateTime.UtcNow.AddDays(14),
                TargetScore = r.GetInt32(5),
                SyllabusCompletedPercent = r.GetInt32(6),
                Notes = r.GetString(7)
            });
        }
        return list;
    }

    public void AddExam(AcademicExam exam)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO academic_exams (subject_id, title, exam_date_utc, target_score, syllabus_completed_percent, notes)
            VALUES ($sid, $title, $date, $target, $syl, $notes);
            """;
        cmd.Parameters.AddWithValue("$sid", exam.SubjectId);
        cmd.Parameters.AddWithValue("$title", exam.Title);
        cmd.Parameters.AddWithValue("$date", exam.ExamDateUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$target", exam.TargetScore);
        cmd.Parameters.AddWithValue("$syl", exam.SyllabusCompletedPercent);
        cmd.Parameters.AddWithValue("$notes", exam.Notes);
        cmd.ExecuteNonQuery();
    }

    public void DeleteExam(long examId)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM academic_exams WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", examId);
        cmd.ExecuteNonQuery();
    }

    #endregion

    #region Flashcards & Spaced Repetition

    public List<Flashcard> GetAllFlashcards()
    {
        var list = new List<Flashcard>();
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, subject_id, question, answer, repetition_level, interval_days, ease_factor, next_review_utc, last_reviewed_utc FROM academic_flashcards;";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new Flashcard
            {
                Id = r.GetInt64(0),
                SubjectId = r.GetInt64(1),
                Question = r.GetString(2),
                Answer = r.GetString(3),
                RepetitionLevel = r.GetInt32(4),
                IntervalDays = r.GetInt32(5),
                EaseFactor = r.GetDouble(6),
                NextReviewUtc = DateTime.TryParse(r.GetString(7), out var d) ? d : DateTime.UtcNow,
                LastReviewedUtc = r.IsDBNull(8) ? null : (DateTime.TryParse(r.GetString(8), out var lr) ? lr : null)
            });
        }
        return list;
    }

    public void AddFlashcard(Flashcard card)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO academic_flashcards (subject_id, question, answer, repetition_level, interval_days, ease_factor, next_review_utc)
            VALUES ($sid, $q, $a, $rep, $int, $ef, $next);
            """;
        cmd.Parameters.AddWithValue("$sid", card.SubjectId);
        cmd.Parameters.AddWithValue("$q", card.Question);
        cmd.Parameters.AddWithValue("$a", card.Answer);
        cmd.Parameters.AddWithValue("$rep", card.RepetitionLevel);
        cmd.Parameters.AddWithValue("$int", card.IntervalDays);
        cmd.Parameters.AddWithValue("$ef", card.EaseFactor);
        cmd.Parameters.AddWithValue("$next", card.NextReviewUtc.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public void UpdateFlashcardReview(Flashcard card)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE academic_flashcards
            SET repetition_level = $rep, interval_days = $int, ease_factor = $ef, next_review_utc = $next, last_reviewed_utc = $last
            WHERE id = $id;
            """;
        cmd.Parameters.AddWithValue("$rep", card.RepetitionLevel);
        cmd.Parameters.AddWithValue("$int", card.IntervalDays);
        cmd.Parameters.AddWithValue("$ef", card.EaseFactor);
        cmd.Parameters.AddWithValue("$next", card.NextReviewUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$last", card.LastReviewedUtc?.ToString("O") ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("$id", card.Id);
        cmd.ExecuteNonQuery();
    }

    public void DeleteFlashcard(long cardId)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM academic_flashcards WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", cardId);
        cmd.ExecuteNonQuery();
    }

    #endregion
}
