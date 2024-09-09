# Image Formatter

## Usage
### Automated Tests
```sh
dotnet test
```

### Manual Tests
```sh
dotnet run --project ImgFormat/ImgFormat.csproj
```

### Build and Deploy with Podman
#### Build
```sh
podman build --tag localhost/imgformat ./ImgFormat
podman save --format=oci-archive --output=./ImgFormat.tar imgformat
```
#### Deploy
```sh
podman load -i ImgFormat.tar
podman run --rm --publish HOST_PORT:8080 imgformat
```

## Background
### Assignment Spec
> Build a service that allows images to be uploaded.
> Once uploaded the image should be encoded and resized in different formats,
> and stored in a database.
> Once those images are ready, they should be accessible in a browser via a URL.

### Requirements
- Inferring from "uploaded" that requests must be accepted over a network
  socket.
- Protocol not specified, but must be supported by browsers.
- Minimum valid requests:
  - Upload image
  - Download image
- Images must be processed & stored in a database when uploaded;
  inferring that the processing of images cannot be deferred until a specific
  resolution & format is requested.
- Supported formats & resolutions are not specified.

### Implementation
- Receive requests over HTTP/S; common, well-supported protocol.
- Start from ASP.NET template for server-side processing & simple HTML UI.
  - HTML form to upload images easily.
  - "Gallery" of available files.
- SQLite database; easy to prototype with, can be swapped for another SQL DB
  if required.
- Storing images in database, despite usually not being advisable.
  - Resets whenever the server goes down, useful for testing.
  - Less risk of clutter in the repo.
  - No need to implement/configure a barebones CDN.
- Limit number of retained images.
  - Small number is sufficient for proof-of-concept, and lessens possibility of
    mess to clean up.
- PNG and JPEG arbitrarily chosen as suitable image formats.
- Downscale to half-resolution and 100x100 "thumbnail" sizes. Not handling edge
  cases where images are already smaller than 100x100.

