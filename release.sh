#!/bin/bash

COMPONENT=$(ls *.nuspec | tr -d '\r' | awk -F. '{ print $1 }')
VERSION=$(grep -m1 "<version>" *.nuspec | tr -d '\r' | sed 's/[ ]//g' | awk -F ">" '{ print $2 }' | awk -F "<" '{ print $1 }')
ID=$(grep -m1 "<id>" *.nuspec | tr -d '\r' | sed 's/[ ]//g' | awk -F ">" '{ print $2 }' | awk -F "<" '{ print $1 }')

SPEC=$COMPONENT.nuspec
PACKAGE=$ID.$VERSION.nupkg

# Any subsequent(*) commands which fail will cause the shell script to exit immediately
set -e
set -o pipefail

# Remove build files
rm -rf ./$PACKAGE

# Build package
nuget pack $SPEC

# Push to nuget repo
nuget push $PACKAGE -Source https://www.nuget.org/api/v2/package