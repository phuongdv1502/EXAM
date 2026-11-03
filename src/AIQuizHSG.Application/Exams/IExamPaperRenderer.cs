using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Exams;

public interface IExamPaperRenderer
{
    byte[] Pdf(Exam exam, ExamVariant variant);
}
