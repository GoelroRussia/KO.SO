using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System.Collections.Generic;

namespace SteelBar.Utils
{
    public static class RebarHelper
    {
        // B1: Lấy tất cả thép nằm trong 1 Assembly
        public static List<Rebar> GetRebarsInAssembly(Document doc, AssemblyInstance assembly)
        {
            var rebars = new List<Rebar>();
            if (assembly == null) return rebars;

            // Lấy toàn bộ các phần tử thuộc Assembly
            ICollection<ElementId> memberIds = assembly.GetMemberIds();
            foreach (ElementId id in memberIds)
            {
                if (doc.GetElement(id) is Rebar rebar)
                {
                    rebars.Add(rebar);
                }
            }
            return rebars;
        }

        // B2: Pick new host cho tất cả thép vừa được chọn
        public static void ChangeRebarsHost(Document doc, List<Rebar> rebars, Element newHost)
        {
            using Transaction tx = new Transaction(doc, "Pick New Host For Rebars");
            tx.Start();

            foreach (Rebar rebar in rebars)
            {
                // Thay đổi HostId của thanh thép sang đối tượng mới
                rebar.SetHostId(doc, newHost.Id);
            }

            tx.Commit();
        }
    }
}