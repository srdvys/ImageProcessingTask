namespace Backend.Models
{
    public enum ImageTaskStatus
    {
        Pending,    //в черзі
        Processing, //в процесі
        Completed,  //завершено
        Failed,     //помилка 
        Cancelled   //скасовано користувачем
    }
}