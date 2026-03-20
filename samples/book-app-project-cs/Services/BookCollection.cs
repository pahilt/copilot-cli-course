using System.Text.Json;
using BookApp.Models;
using LiteDB;

namespace BookApp.Services;

/// <summary>
/// Manages a persistent collection of books stored as JSON on disk.
/// </summary>
/// <remarks>
/// Books are loaded from the data file on construction and saved automatically
/// after every mutating operation. If the data file is missing, the collection
/// starts empty. If the file is corrupted, a warning is written to the console
/// and the collection starts empty.
/// </remarks>
public class BookCollection : IDisposable
{
    private readonly string _dbPath;
    private readonly string _jsonPath;
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<Book> _col;

    /// <summary>
    /// Initializes a new instance of <see cref="BookCollection"/> using a LiteDB file.
    /// If a JSON data file exists, it will be migrated into the DB on first open.
    /// </summary>
    /// <param name="path">Optional path. If it ends with '.db' it is used as the DB file.
    /// If it ends with '.json' it is treated as the legacy JSON data file to migrate.
    /// When null, defaults to 'books.db' in the application base directory and 'data.json' for legacy JSON.</param>
    public BookCollection(string? path = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _dbPath = Path.Combine(AppContext.BaseDirectory, "books.db");
            _jsonPath = Path.Combine(AppContext.BaseDirectory, "data.json");
        }
        else if (Path.GetExtension(path).Equals(".db", StringComparison.OrdinalIgnoreCase))
        {
            _dbPath = path;
            _jsonPath = Path.ChangeExtension(path, ".json");
        }
        else if (Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            _jsonPath = path;
            _dbPath = Path.ChangeExtension(path, ".db");
        }
        else
        {
            // default to db extension if unspecified
            _dbPath = path + ".db";
            _jsonPath = Path.ChangeExtension(_dbPath, ".json");
        }

        _db = new LiteDatabase($"Filename={_dbPath};Connection=shared");
        _col = _db.GetCollection<Book>("books");
        _col.EnsureIndex(b => b.Title, false);
        _col.EnsureIndex(b => b.Author, false);

        // Migrate JSON data if present and DB is empty
        try
        {
            if (File.Exists(_jsonPath) && _col.Count() == 0)
            {
                var json = File.ReadAllText(_jsonPath);
                var books = System.Text.Json.JsonSerializer.Deserialize<List<Book>>(json) ?? new List<Book>();
                if (books.Count > 0)
                {
                    _col.InsertBulk(books);
                    // rename original file to avoid double-migration
                    var migrated = _jsonPath + ".migrated";
                    File.Move(_jsonPath, migrated);
                }
            }
        }
        catch (JsonException)
        {
            Console.WriteLine("Warning: legacy JSON is corrupted. Skipping migration.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Warning: migration I/O error: {ex.Message}");
        }
    }

    /// <summary>Gets a read-only snapshot of all books.</summary>
    public IReadOnlyList<Book> Books => _col.FindAll().ToList();

    public void Dispose()
    {
        _db?.Dispose();
    }

    public Book AddBook(string title, string author, int year)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title cannot be empty.", nameof(title));
        if (string.IsNullOrWhiteSpace(author)) throw new ArgumentException("Author cannot be empty.", nameof(author));
        if (year < 1 || year > DateTime.Now.Year + 1)
            throw new ArgumentOutOfRangeException(nameof(year), $"Year must be between 1 and {DateTime.Now.Year + 1}.");

        var book = new Book { Title = title, Author = author, Year = year };
        _col.Insert(book);
        return book;
    }

    public List<Book> ListBooks() => _col.FindAll().ToList();

    public Book? FindBookByTitle(string title)
    {
        if (title is null) return null;
        return _col.FindOne(b => b.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
    }

    public bool MarkAsRead(string title)
    {
        var book = FindBookByTitle(title);
        if (book is null) return false;
        book.Read = true;
        _col.Update(book);
        return true;
    }

    public bool RemoveBook(string title)
    {
        var book = FindBookByTitle(title);
        if (book is null) return false;
        return _col.DeleteMany(b => b.Title.Equals(title, StringComparison.OrdinalIgnoreCase)) > 0;
    }

    public List<Book> FindByAuthor(string author)
    {
        if (author is null) return new List<Book>();
        return _col.Find(b => b.Author.Equals(author, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
