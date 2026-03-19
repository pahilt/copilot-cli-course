using BookApp.Services;

namespace BookApp.Tests;

public class BookCollectionTests : IDisposable
{
    private readonly string _tempFile;
    private readonly BookCollection _collection;

    public BookCollectionTests()
    {
        // use a unique .db file per test run
        _tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".db");
        if (File.Exists(_tempFile)) File.Delete(_tempFile);
        _collection = new BookCollection(_tempFile);
    }

    public void Dispose()
    {
        try
        {
            _collection?.Dispose();
        }
        catch { }

        try
        {
            if (File.Exists(_tempFile)) File.Delete(_tempFile);
            var migrated = Path.ChangeExtension(_tempFile, ".json.migrated");
            if (File.Exists(migrated)) File.Delete(migrated);
        }
        catch { }
    }

    [Fact]
    public void AddBook_ShouldAddAndPersist()
    {
        var initialCount = _collection.Books.Count;
        _collection.AddBook("1984", "George Orwell", 1949);

        Assert.Equal(initialCount + 1, _collection.Books.Count);

        var book = _collection.FindBookByTitle("1984");
        Assert.NotNull(book);
        Assert.Equal("George Orwell", book.Author);
        Assert.Equal(1949, book.Year);
        Assert.False(book.Read);
    }

    [Fact]
    public void AddBook_EmptyTitle_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => _collection.AddBook("", "Author", 2000));
        Assert.Throws<ArgumentException>(() => _collection.AddBook("   ", "Author", 2000));
    }

    [Fact]
    public void AddBook_EmptyAuthor_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => _collection.AddBook("Title", "", 2000));
        Assert.Throws<ArgumentException>(() => _collection.AddBook("Title", "   ", 2000));
    }

    [Fact]
    public void AddBook_InvalidYear_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _collection.AddBook("Title", "Author", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => _collection.AddBook("Title", "Author", -100));
        Assert.Throws<ArgumentOutOfRangeException>(() => _collection.AddBook("Title", "Author", DateTime.Now.Year + 2));
    }

    [Fact]
    public void AddBook_ShouldPersistAcrossReload()
    {
        _collection.AddBook("Dune", "Frank Herbert", 1965);

        var reloaded = new BookCollection(_tempFile);
        var book = reloaded.FindBookByTitle("Dune");

        Assert.NotNull(book);
        Assert.Equal("Frank Herbert", book.Author);
        Assert.Equal(1965, book.Year);
    }

    [Fact]
    public void MarkAsRead_ShouldSetReadTrue()
    {
        _collection.AddBook("Dune", "Frank Herbert", 1965);
        var result = _collection.MarkAsRead("Dune");

        Assert.True(result);
        Assert.True(_collection.FindBookByTitle("Dune")!.Read);
    }

    [Fact]
    public void MarkAsRead_NonexistentBook_ShouldReturnFalse()
    {
        var result = _collection.MarkAsRead("Nonexistent Book");
        Assert.False(result);
    }

    [Fact]
    public void RemoveBook_ShouldRemoveExistingBook()
    {
        _collection.AddBook("The Hobbit", "J.R.R. Tolkien", 1937);
        var result = _collection.RemoveBook("The Hobbit");

        Assert.True(result);
        Assert.Null(_collection.FindBookByTitle("The Hobbit"));
    }

    [Fact]
    public void RemoveBook_NonexistentBook_ShouldReturnFalse()
    {
        var result = _collection.RemoveBook("Nonexistent Book");
        Assert.False(result);
    }

    [Fact]
    public void FindByAuthor_ShouldReturnMatchingBooks()
    {
        _collection.AddBook("Dune", "Frank Herbert", 1965);
        _collection.AddBook("Dune Messiah", "Frank Herbert", 1969);
        _collection.AddBook("1984", "George Orwell", 1949);

        var results = _collection.FindByAuthor("Frank Herbert");

        Assert.Equal(2, results.Count);
        Assert.All(results, b => Assert.Equal("Frank Herbert", b.Author));
    }

    [Fact]
    public void FindByAuthor_CaseInsensitive_ShouldMatch()
    {
        _collection.AddBook("Dune", "Frank Herbert", 1965);

        var results = _collection.FindByAuthor("frank herbert");

        Assert.Single(results);
    }

    [Fact]
    public void FindByAuthor_NoMatch_ShouldReturnEmpty()
    {
        _collection.AddBook("1984", "George Orwell", 1949);

        var results = _collection.FindByAuthor("Unknown Author");

        Assert.Empty(results);
    }

    [Fact]
    public void ListBooks_ShouldReturnCopy()
    {
        _collection.AddBook("1984", "George Orwell", 1949);

        var list = _collection.ListBooks();
        list.Clear(); // mutate the returned copy

        Assert.Single(_collection.Books); // original should be unaffected
    }
}
