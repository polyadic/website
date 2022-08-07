open System.IO
open System.Net.Http
open System.Net.Http.Json
open System.Text.Json
open System

let outputDirectory = "_site"
let indexHtml = "index.html"

let getLatestPackageVersion package =
    async {
        use httpClient = new HttpClient()
        let! document = httpClient.GetFromJsonAsync<JsonDocument>($"https://api-v2v3search-0.nuget.org/query?q={package}&skip=0&take=1&prerelease=false&semVerLevel=2.0.0") |> Async.AwaitTask
        return (document.RootElement.GetProperty("data")[0]).GetProperty("version").GetString()
    }

let getLatestFunckyVersion() = getLatestPackageVersion "Funcky"

let renderIndexHtml (template: string, packageVersion, year) =
    template
        .Replace("{{PackageVersion}}", packageVersion)
        .Replace("{{Year}}", year.ToString())

let writeIndexHtml() =
    let version = getLatestFunckyVersion() |> Async.RunSynchronously
    let indexHtmlContents = renderIndexHtml(File.ReadAllText(indexHtml), version, DateTime.Today.Year)
    File.WriteAllText(Path.Combine(outputDirectory, indexHtml), contents = indexHtmlContents)

let copyFileToOutput source =
    let relativePath = Path.GetRelativePath(relativeTo = Environment.CurrentDirectory, path = source)
    let target = Path.Combine(outputDirectory, relativePath)
    Directory.CreateDirectory(Path.GetDirectoryName(target)) |> ignore
    File.Copy(source, target, overwrite = true)

let isFileInOutputDirectory path =
    let absoluteTargetPath = Path.GetFullPath(outputDirectory) + Path.DirectorySeparatorChar.ToString()
    let absolutePath = Path.GetFullPath(path)
    absolutePath.StartsWith(absoluteTargetPath)

let copyToOutput (pattern) =
    Directory.GetFiles(Environment.CurrentDirectory, searchPattern = pattern, searchOption = SearchOption.AllDirectories)
        |> Seq.where (fun p -> not (isFileInOutputDirectory p))
        |> Seq.iter copyFileToOutput

Directory.CreateDirectory(outputDirectory)
writeIndexHtml()
copyToOutput("fonts/*.woff2")
copyToOutput("icons/*.svg")
copyToOutput("*.css")
copyToOutput("*.js")