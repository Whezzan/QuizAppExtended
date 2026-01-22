using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

public class OpenTriviaService
{
    private readonly HttpClient _client = new HttpClient();

    public async Task<List<TriviaQuestion>> GetQuestionsAsync(int amount, string category, string difficulty)
    {
        string url = $"https://opentdb.com/api.php?amount={amount}";

        if (!string.IsNullOrEmpty(category))
            url += $"&category={category}";
        if (!string.IsNullOrEmpty(difficulty))
            url += $"&difficulty={difficulty}";

        url += "&type=multiple";

        var response = await _client.GetFromJsonAsync<TriviaResponse>(url);

        return response?.results ?? new List<TriviaQuestion>();
    }
}

public class TriviaResponse
{
    public int response_code { get; set; }
    public List<TriviaQuestion> results { get; set; }
}

public class TriviaQuestion
{
    public string category { get; set; }
    public string difficulty { get; set; }
    public string question { get; set; }
    public string correct_answer { get; set; }
    public List<string> incorrect_answers { get; set; }
}