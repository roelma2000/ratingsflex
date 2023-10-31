namespace ratingsflex.Areas.Movies.Models
{
    public class ManageUploadedFilesViewModel
    {
        public List<ManageUploadedFile> UploadedFiles { get; set; } = new List<ManageUploadedFile>();
    }

    public class ManageUploadedFile
    {
        public int Id { get; set; }
        public string FileTitle { get; set; }
        public bool IsAssigned { get; set; }
    }
}
