public class Document
{
    public int DocID { get; set; }
    public int DocTypeID { get; set; }
    public string DocName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = "Pending";
    public int UserID { get; set; }
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
    public DocumentType DocType { get; set; } = null!;
}

public class DocumentType
{
    public int DocTypeID { get; set; }
    public string TypeName { get; set; } = string.Empty;
}