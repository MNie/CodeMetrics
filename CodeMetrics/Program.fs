open FSharp.Data
open System
open System.IO
 
type MostlyChanged = CsvProvider<"data\mostlychanged.csv", ",">
type LastChanged = CsvProvider<"data\lastchanged.csv", ",">
type Metrics = CsvProvider<"data\codemetrics.csv", ",">
 
type Info =
    {
        Name: string
        Complexity: int
        FileName: string
        LastEdit: DateTime
        HowManyChanges: int
    }

[<Literal>]
let outputPath = "output.csv"
 
[<EntryPoint>]
let main argv =
    let mostly = MostlyChanged.GetSample()
    let last = LastChanged.GetSample()
    let data = Metrics.GetSample()
    let fourWeeksAgo = DateTime.UtcNow.Date.AddDays(-28.)
 
    let fileInformation forClass = 
        let byName (y: string) = y.EndsWith(sprintf "%s.cs" forClass)
        let lastChange = last.Rows |> Seq.tryFind(fun y -> byName y.File)
        let howManyChanges = mostly.Rows |> Seq.tryFind(fun y -> byName y.File)
 
        let fileName =
            if lastChange.IsNone then
                if howManyChanges.IsNone then
                    "File not found"
                else howManyChanges.Value.File
            else lastChange.Value.File

        let lastChange =
            if lastChange.IsNone then DateTime.MinValue
            else lastChange.Value.``Last Change``
        
        let numberOfChanges =
            if howManyChanges.IsNone then 0
            else howManyChanges.Value.``Number of changes``
        
        (fileName, lastChange, numberOfChanges)

    let metrics =
        data.Rows
        |> Seq.filter(fun x -> x.Scope = "Member")
        |> Seq.map(fun x ->
            let fileName, lastChange, numberOfChanges = fileInformation x.Type
            {
                Name = x.Member;
                Complexity = x.``Cyclomatic Complexity``;
                FileName = fileName;
                LastEdit = lastChange;
                HowManyChanges = numberOfChanges;
            }
        )
        |> Seq.filter(fun x -> 
            x.LastEdit >= fourWeeksAgo &&
            x.Complexity > 5 &&
            x.HowManyChanges > 2
        )
        |> Seq.sortByDescending(fun x -> x.Complexity)
        |> Seq.map(fun x -> File.AppendAllText(outputPath, sprintf "%s, %d, %s, %d, %A\n" x.Name x.Complexity x.FileName x.HowManyChanges x.LastEdit))
        |> Seq.toList

    0