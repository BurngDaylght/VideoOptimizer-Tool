using SFB;

public class FileExtensionsConfig
{
    public readonly ExtensionFilter[] InputFormats =
    {
        new ExtensionFilter("All Supported Video Files",
            "mp4","mkv","avi","mov","wmv","flv","webm","m4v","mpg","mpeg","3gp","ts","mts","m2ts"),

        new ExtensionFilter("MP4", "mp4"),
        new ExtensionFilter("MKV", "mkv"),
        new ExtensionFilter("AVI", "avi"),
        new ExtensionFilter("MOV", "mov"),
        new ExtensionFilter("WMV", "wmv"),
        new ExtensionFilter("WebM", "webm"),
        new ExtensionFilter("MPEG", "mpg", "mpeg"),
        new ExtensionFilter("3GP", "3gp"),
        new ExtensionFilter("Transport Stream", "ts", "mts", "m2ts"),
    };

    public readonly ExtensionFilter[] OutputFormats =
    {
        new ExtensionFilter("MP4 (H.264) ", "mp4"),
        new ExtensionFilter("MKV (H.264) ", "mkv"),
        new ExtensionFilter("AVI ", "avi"),
        new ExtensionFilter("MOV ", "mov"),
        new ExtensionFilter("WebM (VP9) ", "webm")
    };
}
