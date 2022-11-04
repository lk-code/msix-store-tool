# msix-store-tool

![MsixStoreTool](https://raw.githubusercontent.com/lk-code/msix-store-tool/main/icon_128.png)

[![.NET Version](https://img.shields.io/badge/dotnet%20version-net6.0-blue?style=flat-square)](http://www.nuget.org/packages/hetznercloudapi/)
[![License](https://img.shields.io/github/license/lk-code/msix-store-tool.svg?style=flat-square)](https://github.com/lk-code/msix-store-tool/blob/master/LICENSE)
[![Build](https://github.com/lk-code/msix-store-tool/actions/workflows/dotnet.yml/badge.svg)](https://github.com/lk-code/msix-store-tool/actions/workflows/dotnet.yml)
[![Downloads](https://img.shields.io/nuget/dt/msixtool.svg?style=flat-square)](http://www.nuget.org/packages/msixtool/)
[![NuGet](https://img.shields.io/nuget/v/msixtool.svg?style=flat-square)](http://nuget.org/packages/msixtool)

[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=lk-code_msix-store-tool&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=lk-code_msix-store-tool)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=lk-code_msix-store-tool&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=lk-code_msix-store-tool)

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
