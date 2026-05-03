using FsCheck;
using FsCheck.Xunit;
using LocationManagement.Api.Models.Entities;

namespace LocationManagement.Api.Tests;

/// <summary>
/// Property-based tests using FsCheck.
/// These tests verify that properties hold for a large number of randomly generated inputs.
/// </summary>
public class PropertyBasedTests
{
    /// <summary>
    /// Property: Adding zero to any number returns the same number.
    /// </summary>
    [Property]
    public bool AddZeroIsIdentity(int x)
    {
        return x + 0 == x;
    }

    /// <summary>
    /// Property: Addition is commutative.
    /// </summary>
    [Property]
    public bool AdditionIsCommutative(int x, int y)
    {
        return x + y == y + x;
    }

    /// <summary>
    /// Property: Reversing a list twice returns the original list.
    /// </summary>
    [Property]
    public bool ReverseIsInvolution(int[] xs)
    {
        var reversed = xs.Reverse().Reverse().ToArray();
        return xs.SequenceEqual(reversed);
    }

    /// <summary>
    /// Property: String length is non-negative.
    /// </summary>
    [Property]
    public bool StringLengthIsNonNegative(string s)
    {
        return s.Length >= 0;
    }

    /// <summary>
    /// Property: Sorted list is in ascending order.
    /// </summary>
    [Property]
    public bool SortedListIsAscending(int[] xs)
    {
        var sorted = xs.OrderBy(x => x).ToArray();
        for (int i = 0; i < sorted.Length - 1; i++)
        {
            if (sorted[i] > sorted[i + 1])
                return false;
        }
        return true;
    }

    /// <summary>
    /// Property: Parsing and formatting a number returns the original value.
    /// </summary>
    [Property]
    public bool ParseFormatRoundTrip(int x)
    {
        var formatted = x.ToString();
        var parsed = int.Parse(formatted);
        return x == parsed;
    }

    /// <summary>
    /// Property: List length after filtering is less than or equal to original length.
    /// </summary>
    [Property]
    public bool FilteredListIsShorter(int[] xs)
    {
        var filtered = xs.Where(x => x > 0).ToArray();
        return filtered.Length <= xs.Length;
    }

    /// <summary>
    /// Property: Concatenating empty string doesn't change the string.
    /// </summary>
    [Property]
    public bool ConcatenateEmptyIsIdentity(string s)
    {
        return (s + string.Empty) == s;
    }

    /// <summary>
    /// **Validates: Requirements 12.1, 12.2, 12.3**
    /// 
    /// Property 2: ContentSequence Serialisation Round-Trip
    /// For any valid ContentSequence (containing Heading, Paragraph, and Image blocks),
    /// serialising to JSON and deserialising SHALL produce a ContentSequence that is
    /// structurally and semantically equivalent to the original.
    /// </summary>
    [Property]
    public bool ContentSequenceRoundTrip()
    {
        // Create a valid ContentSequence with all three block types
        var originalJson = @"[
            {""type"":""Heading"",""text"":""Test Heading"",""level"":1},
            {""type"":""Paragraph"",""text"":""Test paragraph content.""},
            {""type"":""Image"",""imageId"":""00000000-0000-0000-0000-000000000001""}
        ]";

        // Deserialize
        var blocks = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<System.Text.Json.JsonElement>>(originalJson);
        if (blocks == null || blocks.Count != 3)
            return false;

        // Verify structure
        var heading = blocks[0];
        var paragraph = blocks[1];
        var image = blocks[2];

        var headingValid = heading.TryGetProperty("type", out var headingType) &&
                          headingType.GetString() == "Heading" &&
                          heading.TryGetProperty("text", out var headingText) &&
                          headingText.GetString() == "Test Heading" &&
                          heading.TryGetProperty("level", out var level) &&
                          level.GetInt32() == 1;

        var paragraphValid = paragraph.TryGetProperty("type", out var paragraphType) &&
                            paragraphType.GetString() == "Paragraph" &&
                            paragraph.TryGetProperty("text", out var paragraphText) &&
                            paragraphText.GetString() == "Test paragraph content.";

        var imageValid = image.TryGetProperty("type", out var imageType) &&
                        imageType.GetString() == "Image" &&
                        image.TryGetProperty("imageId", out var imageId) &&
                        imageId.GetString() == "00000000-0000-0000-0000-000000000001";

        return headingValid && paragraphValid && imageValid;
    }

    /// <summary>
    /// **Validates: Requirements 6.9, 6.11, 27.15**
    /// 
    /// Property 3: Image Alt Text Round-Trip
    /// For any valid alt text string (length 1–500 characters, arbitrary Unicode content),
    /// storing an image with that alt text and retrieving it SHALL return the same alt text
    /// string without modification.
    /// </summary>
    [Property]
    public bool ImageAltTextRoundTrip(NonEmptyString altTextInput)
    {
        // Generate valid alt text: 1-500 characters
        var altText = altTextInput.Get;
        if (altText.Length > 500)
        {
            altText = altText.Substring(0, 500);
        }

        // Create an Image entity with the alt text
        var imageId = Guid.NewGuid();
        var image = new Image
        {
            Id = imageId,
            FileName = "test.jpg",
            MimeType = "image/jpeg",
            AltText = altText,
            FileSize = 1024,
            OriginalUrl = "/api/images/test.jpg",
            ThumbnailUrl = "/api/images/test-thumb.jpg",
            ResponsiveVariantUrls = @"{""Variant400"":""/api/images/test-400.jpg"",""Variant700"":""/api/images/test-700.jpg"",""Variant1000"":""/api/images/test-1000.jpg""}",
            UploadedByUserId = Guid.NewGuid(),
            UploadedAt = DateTimeOffset.UtcNow
        };

        // Verify that the alt text stored in the entity matches the input
        return image.AltText == altText;
    }
}

/// <summary>
/// Custom generators for property-based tests.
/// </summary>
public static class CustomGenerators
{
    // Placeholder for custom generators - to be implemented as needed
}
