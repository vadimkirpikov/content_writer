using System.Text;
using OpenQA.Selenium;
using System.Text.RegularExpressions;
using OpenQA.Selenium.Chrome;
using Microsoft.EntityFrameworkCore;


string langName = "csharp";
var chromeDriver = new ChromeDriver("C:\\Users\\Sveta\\source\\repos\\writr\\chromedriver.exe");
var response = 
    DoRequest(chromeDriver, 
        "Подробная обучающая статья без раговорных ответов на запрос по теме 'классы C# и .NET 8.0' " +
                                       "минимум 3000 символов, каждый подзаголовок обозначается символами ##. У статьи не должно быть начального заголовка", langName);
Console.WriteLine(response);
using (var db = new ApplicationContext())
{
    db.Tutorials.Add(new(langName, "general", "dicts", response));
    db.SaveChanges();
}

string DoRequest(IWebDriver driver, string text, string langName)
{
    driver.Navigate().GoToUrl("https://trychatgpt.ru/");
    var searchLine = driver.FindElement(By.Id("input"));
    var inputButton = driver.FindElement(By.Id("send"));
    Thread.Sleep(5000);
    searchLine.SendKeys(text);
    var js = (IJavaScriptExecutor)driver;
    js.ExecuteScript("window.scrollBy(0, 200);");
    Thread.Sleep(14000);
    inputButton.Click();
    Thread.Sleep(80000);
    var messages = driver.FindElements(By.ClassName("bot"));
    var response = messages[^1].Text.GetClearText().ExtractCode(langName);
    var matches = Regex.Matches(response, @"`([^`]+)`");
    foreach (var i in matches)
    {
        response = response.Replace(i.ToString(), "<b>" + i.ToString().Substring(1, i.ToString().Length-2) + "</b>");
    }
    //response = response.MakeRegexReplace(@"`([^`]+)`", "<b>", "</b>");
    var bytes = Encoding.UTF8.GetBytes(response);
    response = Encoding.UTF8.GetString(bytes);
    driver.Quit();
    return response;
}
public static class Extension
{
    public static bool IsInvalid(this string text)
        => text.Contains("</code></pre></div>") & !text.Contains("<div><pre><code");
    public static bool IsCode(this string text)
        => text.Contains("<div><pre><code");
    public static string GetClearText(this string text)
        => text.Substring(20, text.Length - 51).Trim();

    public static string MakeRegexReplace(this string text, string pattern, string start, string end)
    {
        var matches = Regex.Matches(text, pattern);
        foreach (var line in matches)
        {
            text = text.Replace(line.ToString(), start + line + end);
        }
        return text;
    }

    public static string MakeHeader(this string text)
    {
        var s = Regex.Match(text, @"^# (.+)?\r\n").ToString();
        text = text.Replace(s, "<h2 class='header'>" + s.Substring(2, s.IndexOf("<br>")-2) + "</h2>");
        return text;
    }
    public static string ExtractSymbols(this string text)
    {
        var matches = Regex.Matches(text, @"#(#+.*?)\r\n");
        foreach (var l in matches)
        {
            var line = l.ToString();
            var start = line.IndexOf(' ')+1;
            var count = line.Length - start-2;
            text = text.Replace(line, $"<h4 class='header'>"+line.Substring(start, count)+$"</h4>");
        }
        text = Regex.Replace(text, @"\*(\*+)", "");
        text = Regex.Replace(text, @"\r\n", "<br>");
        return text;
    }

    public static string ExtractCode(this string text, string langName)
    {
        text = text.Replace(">=", "&ge;")
            .Replace("<=", "&le;")
            .Replace(">", "&gt;")
            .Replace("<", "&lt;");
        var textWithCode = Regex.Replace(text, @"\r\n\r\n\r\n", "</code></pre></div>KODDOK");
        textWithCode = Regex.Replace(textWithCode, langName+@"\r\n" , "DOKKOD<div><pre><code class=" + langName + ">");
        var textWithBr = textWithCode.Split("DOK").ToList();
        for (int i = 1; i < textWithBr.Count-1; i++)
        {
            if (textWithBr[i].IsInvalid() & !textWithBr[i - 1].IsInvalid() & textWithBr[i + 1].IsInvalid())
            {
                textWithBr[i] = textWithBr[i].Replace("</code></pre></div>KOD", "KOD<div><pre><code class=no-highlight>");
            }
        }

        var newString = string.Join("", textWithBr);
        var resultText = newString.Split("KOD")
            .Select(line => line.IsCode() ? line : line.ExtractSymbols());
        var result = string.Join("", resultText);
        return result;
    }

    


    public static void GetLines(this string text)
    {
        foreach (var line in text.Split('\r'))
        {
            Console.WriteLine("length "+line.Length+"\nBytes: ");
            var bytes = Encoding.Default.GetBytes(line);
            foreach (var bt in bytes)
            {
                Console.Write(bt+" ");
            }

            Console.WriteLine();
        }
    }

    public static string Encode1(this string text)
        => Encoding.UTF8.GetString(Encoding.Unicode.GetBytes(text));

    public static int GetIndexOfTwoEndlines(this string text)
        => text.IndexOf("\n\n");

    public static void GetEndlineIndees(this string text)
    {
        var count = 0;
        foreach (var symbol in text)
        {
            if (symbol.CompareTo('\n')==0) Console.WriteLine(count);
            count++;
        }
    }
}

public class Tutorial
{
    public int Id { get; set; }
    public string LangName { get; set;}
    public string ShortName { get; set; }
    public string TextOfTutorial { get; set; }
    public string Section { get; set; }

    public Tutorial(string langName, string section, string shortName, string textOfTutorial)
    {
        LangName = langName;
        Section = section;
        ShortName = shortName;
        TextOfTutorial = textOfTutorial;
    }
}

public class ApplicationContext : DbContext
{
    public DbSet<Tutorial> Tutorials { get; set; } = null;

    public ApplicationContext()
    {
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySQL("server=localhost;database=programming_site;user=root;password=texus-find12345VadQWE#;charset=utf8mb4;");
    }
}