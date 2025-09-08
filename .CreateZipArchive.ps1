param(
    [string]$DirsToZip,
    [string]$ZipName,
    [string]$RootDir
)

# Загружаем необходимые сборки
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Проверяем существование корневой директории
if (-not (Test-Path $RootDir)) {
    Write-Error "Корневая директория '$RootDir' не существует"
    exit 1
}

# Создаем временный файл для архива
$tempZipPath = [System.IO.Path]::GetTempFileName()
$tempZipPath = $tempZipPath -replace '\.tmp$', '.zip'

try {
    # Создаем пустой архив через временную папку
    $tempDir = [System.IO.Path]::GetTempPath()
    $emptyTempDir = Join-Path $tempDir ([System.Guid]::NewGuid().ToString())
    New-Item -ItemType Directory -Path $emptyTempDir | Out-Null
    
    try {
        # Создаем пустой архив из пустой директории
        [System.IO.Compression.ZipFile]::CreateFromDirectory($emptyTempDir, $tempZipPath)
    }
    finally {
        # Удаляем временную папку
        if (Test-Path $emptyTempDir) {
            Remove-Item -Path $emptyTempDir -Recurse -Force
        }
    }
    
    # Открываем архив для добавления файлов
    $zipArchive = [System.IO.Compression.ZipFile]::Open($tempZipPath, [System.IO.Compression.ZipArchiveMode]::Update)
    
    try {
        # Разбиваем список директорий по пробелам
        $directories = $DirsToZip -split '\s+' | Where-Object { $_ -ne '' }
        
        foreach ($dir in $directories) {
            # Если директория это ".", добавляем всё содержимое RootDir
            if ($dir -eq ".") {
                $sourcePath = $RootDir
                $archiveBasePath = ""
            } else {
                $sourcePath = Join-Path $RootDir $dir
                # Для архива используем forward slashes
                $archiveBasePath = $dir -replace '\\', '/'
            }
            
            # Проверяем существование директории
            if (-not (Test-Path $sourcePath -PathType Container)) {
                Write-Warning "Folder '$sourcePath' does not exists, skipping"
                continue
            }
            
            Write-Host "Adding folder: $sourcePath"
            
            # Получаем все файлы в директории рекурсивно
            $files = Get-ChildItem -Path $sourcePath -Recurse -File
            
            foreach ($file in $files) {
                # Вычисляем относительный путь от sourcePath
                $relativePath = $file.FullName.Substring($sourcePath.Length).TrimStart('\', '/')
                
                # Формируем путь в архиве
                if ($archiveBasePath -eq "") {
                    $entryName = $relativePath
                } else {
                    $entryName = Join-Path $archiveBasePath $relativePath
                }
                
                # Заменяем обратные слеши на прямые для правильного формата ZIP
                $entryName = $entryName -replace '\\', '/'
                
                # Удаляем существующую запись, если есть
                $existingEntry = $zipArchive.GetEntry($entryName)
                if ($existingEntry) {
                    $existingEntry.Delete()
                }
                
                # Добавляем файл в архив
                [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zipArchive, $file.FullName, $entryName, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
            }
        }
        
        Write-Host "Success: $tempZipPath"
    }
    finally {
        # Закрываем архив
        if ($zipArchive) {
            $zipArchive.Dispose()
        }
    }
    
    # Перемещаем архив в конечное место назначения
    if (Test-Path $ZipName) {
        Remove-Item -Path $ZipName -Force
    }
    
    # Создаем директорию для архива, если она не существует
    $zipDir = Split-Path $ZipName -Parent
    if ($zipDir -and -not (Test-Path $zipDir)) {
        New-Item -ItemType Directory -Path $zipDir | Out-Null
    }
    
    Move-Item -Path $tempZipPath -Destination $ZipName -Force
    Write-Host "Archive saved as: $ZipName"
}
catch {
    Write-Error "Error: $_"
    # Удаляем временный файл в случае ошибки
    if (Test-Path $tempZipPath) {
        Remove-Item -Path $tempZipPath -Force
    }
}
