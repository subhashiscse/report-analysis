// namespace ReportAnalysis;
//
// public class PoiZurich
// {
//     public int Id { get; set; }
//     public string Wkt_Building { get; set; }
// }
//

using System.ComponentModel.DataAnnotations.Schema;

[Table("poi_zurich_ch")]
public class PoiZurich
{
    [Column("id")]
    public int Id { get; set; }

    [Column("wkt_building")]
    public string Wkt_Building { get; set; }
}
