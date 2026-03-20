namespace BookApp.Models;

/// <summary>
/// Represents a book in the collection.
/// </summary>
public class Book
{
    /// <summary>Gets the title of the book.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Gets the name of the book's author.</summary>
    public string Author { get; init; } = string.Empty;

    /// <summary>Gets the year the book was published.</summary>
    public int Year { get; init; }

    /// <summary>Gets or sets a value indicating whether the book has been read.</summary>
    public bool Read { get; set; }
}
