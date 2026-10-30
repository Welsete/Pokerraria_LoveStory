using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
// Prints the constants of Terraria.ID.ItemID / TileID / WallID / PaintID from tModLoader.dll
using var fs = File.OpenRead(args[0]);
using var pe = new PEReader(fs);
var md = pe.GetMetadataReader();
var wanted = new HashSet<string> { "ItemID", "TileID", "WallID", "PaintID" };
foreach (var th in md.TypeDefinitions)
{
    var t = md.GetTypeDefinition(th);
    var name = md.GetString(t.Name);
    if (md.GetString(t.Namespace) != "Terraria.ID" || !wanted.Contains(name)) continue;
    foreach (var fh in t.GetFields())
    {
        var f = md.GetFieldDefinition(fh);
        var ch = f.GetDefaultValue();
        if (ch.IsNil) continue;
        var c = md.GetConstant(ch);
        var r = md.GetBlobReader(c.Value);
        object v = c.TypeCode switch
        {
            ConstantTypeCode.Int16 => r.ReadInt16(), ConstantTypeCode.UInt16 => r.ReadUInt16(),
            ConstantTypeCode.Int32 => r.ReadInt32(), ConstantTypeCode.Byte => r.ReadByte(), _ => "?"
        };
        Console.WriteLine($"IDDUMP {name} {md.GetString(f.Name)} {v}");
    }
}
