# msix-store-tool

![MsixStoreTool](https://raw.githubusercontent.com/lk-code/msix-store-tool/main/icon_128.png)

A small tool (especially for WinUI 3 projects) to easily create a MSIXBUNDLE file for the Microsoft Store from several MSIX files.

This tool is based on this manual (as of October 2022) [bundle-msix-packages](https://learn.microsoft.com/en-us/windows/msix/packaging-tool/bundle-msix-packages).

## install the tool

`dotnet tool install --global msixtool`

## update the tool

`dotnet tool update --global msixtool`

## usage

### bundle msix-files to a single msixbundle for microsoft store

bundles all msix-files in the directory to a single msixbundle and signs it for store-upload

`msixtool bundle "{msix-source-directory}" "{msixbundle-filename}" "{path-to-pfx}" {hash-algorithm}`

**example**:

`msixtool bundle "D:\AppPackages" "MyApp_1.0.13.0.msixbundle" "D:\MyApp.pfx" SHA-256`
