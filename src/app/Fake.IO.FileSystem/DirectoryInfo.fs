namespace Fake.IO.FileSystem

open System.IO
open Fake.IO.FileSystem.Operators

module DirectoryInfo =
    /// Creates a DirectoryInfo for the given path.
    let inline OfPath path = DirectoryInfo(path)

    /// Gets all subdirectories of a given directory.
    let inline SubDirectories (dir : DirectoryInfo) = dir.GetDirectories()

    /// Gets all files in the directory.
    let inline Files (dir : DirectoryInfo) = dir.GetFiles()

    /// Finds all the files in the directory matching the search pattern.
    let MatchingFiles pattern (dir : DirectoryInfo) = 
        if dir.Exists then dir.GetFiles pattern
        else [||]

        
    /// Finds all the files in the directory and in all subdirectories matching the search pattern.
    let MatchingFilesRecursive pattern (dir : DirectoryInfo) = 
        if dir.Exists then dir.GetFiles(pattern, SearchOption.AllDirectories)
        else [||]
        
    /// Checks if dir1 is a subfolder of dir2. If dir1 equals dir2 the function returns also true.
    let rec IsSubfolderOf (dir2 : DirectoryInfo) (dir1 : DirectoryInfo) = 
        if Path.normalizeFileName dir1.FullName = Path.normalizeFileName dir2.FullName then true
        else if isNull dir1.Parent then false
        else dir1.Parent |> IsSubfolderOf dir2
        
    /// Checks if the file is in a subfolder of the dir.
    let ContainsFile (fileInfo : FileInfo) (dir : DirectoryInfo) = IsSubfolderOf dir fileInfo.Directory
    
    /// Checks if the directory exists on disk.
    let Exists (dir : DirectoryInfo) = dir.Exists
    
    /// Ensure that directory chain exists. Create necessary directories if necessary.
    let inline Ensure (dir : DirectoryInfo) =
        if not dir.Exists then dir.Create()
 
    /// Performs the given actions on all files and subdirectories
    let rec private recursively dirF fileF (dir : DirectoryInfo) = 
        dir
        |> SubDirectories
        |> Seq.iter (fun dir -> 
               recursively dirF fileF dir
               dirF dir)
        dir
        |> Files
        |> Seq.iter fileF

    /// Sets the directory readonly 
    let SetReadOnly readOnly (dir : DirectoryInfo) = 
        if dir.Exists then 
            let isReadOnly = dir.Attributes &&& FileAttributes.ReadOnly = FileAttributes.ReadOnly
            if readOnly && (not isReadOnly) then dir.Attributes <- dir.Attributes ||| FileAttributes.ReadOnly
            if (not readOnly) && not isReadOnly then dir.Attributes <- dir.Attributes &&& (~~~FileAttributes.ReadOnly)

    /// Sets all files in the directory readonly recursively.
    let SetReadOnlyRecursive readOnly dir = 
        recursively (SetReadOnly readOnly) (fun file -> file.IsReadOnly <- readOnly) dir
    
    /// Copies the file structure recursively, filtering files.
    let rec CopyRecursiveToWithFilter overwrite filter (outputDir : DirectoryInfo) (dir : DirectoryInfo) = 
        let files = 
            dir
            |> SubDirectories
            |> Seq.fold (fun acc (d : DirectoryInfo) -> 
                   let newDir = outputDir.FullName @@ d.Name
                                |> OfPath
                   Ensure newDir
                   d
                   |> CopyRecursiveToWithFilter overwrite filter newDir
                   |> fun r -> r @ acc) []
        (dir
         |> Files
         |> Seq.filter (fun f -> filter outputDir f)
         |> Seq.map (fun f -> 
                let newFileName = outputDir.FullName @@ f.Name
                f.CopyTo(newFileName, overwrite) |> ignore
                newFileName)
         |> Seq.toList) @ files

    /// Copies the file structure recursively.
    let CopyRecursiveTo overwrite (outputDir : DirectoryInfo) (dir : DirectoryInfo) = CopyRecursiveToWithFilter overwrite (fun _ _ -> true) outputDir dir
