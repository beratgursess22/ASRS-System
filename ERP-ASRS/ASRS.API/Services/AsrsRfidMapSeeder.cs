using ASRS.Core.Entities;
using ASRS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace ASRS.API.Services;

public static class AsrsRfidMapSeeder
{
    public static async Task SeedDefaultMappingsAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        // Kart 1..12 -> (row,col) 1,1 .. 3,4 (DB 0-based)
        var defaults = new (string uid, int row, int col)[]
        {
            ("F3 21 6E 2E", 0, 0),
            ("53 2B 23 21", 0, 1),
            ("33 61 62 2E", 0, 2),
            ("B3 90 3F 36", 0, 3),
            ("C3 66 E4 35", 1, 0),
            ("23 FC EC 35", 1, 1),
            ("13 21 F7 35", 1, 2),
            ("93 DE FA 35", 1, 3),
            ("B3 92 D3 35", 2, 0),
            ("03 0D 5E 2E", 2, 1),
            ("13 DC 0A 36", 2, 2),
            ("A3 7D 1A 4E", 2, 3)
        };

        var now = DateTime.UtcNow;

        var existingMaps = await db.RfidRackMaps.ToListAsync(cancellationToken);

        foreach (var (uid, row, col) in defaults)
        {
            var normalized = RfidUidNormalizer.Normalize(uid);
            var existing = existingMaps.FirstOrDefault(x => x.CardUid == normalized);
            existing ??= existingMaps.FirstOrDefault(x => x.Row == row && x.Col == col);
            if (existing is null)
            {
                db.RfidRackMaps.Add(new RfidRackMap
                {
                    CardUid = normalized,
                    Row = row,
                    Col = col,
                    IsActive = true,
                    UpdatedAt = now
                });
            }
            else
            {
                existing.CardUid = normalized;
                existing.Row = row;
                existing.Col = col;
                existing.IsActive = true;
                existing.UpdatedAt = now;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
