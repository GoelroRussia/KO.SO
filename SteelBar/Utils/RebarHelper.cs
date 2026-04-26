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
        /// <summary>
        /// Thuật toán kiểm tra thanh thép có vi phạm lớp bê tông bảo vệ hay không
        /// </summary>
        public static double GetCoverViolationDistance(Document doc, Rebar rebar)
        {
            Element host = doc.GetElement(rebar.GetHostId());
            if (host == null) return 0;

            double minCover = GetHostMinimumCover(host);
            if (minCover <= 0) return 0;

            double barRadius = 0;
            if (doc.GetElement(rebar.GetTypeId()) is RebarBarType barType)
                barRadius = barType.BarNominalDiameter / 2.0;

            Options opt = new Options { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = true };
            GeometryElement geomElem = host.get_Geometry(opt);
            Solid hostSolid = GetLargestSolid(geomElem);
            if (hostSolid == null) return 0;

            IList<Curve> curves = rebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeAllMultiplanarCurves, 0);
            double maxViolation = 0;

            foreach (Curve curve in curves)
            {
                foreach (XYZ pt in curve.Tessellate())
                {
                    foreach (Face face in hostSolid.Faces)
                    {
                        IntersectionResult result = face.Project(pt);
                        if (result != null)
                        {
                            // Khoảng cách thông thủy thực tế từ mép thép tới mặt bê tông
                            double clearDistance = result.Distance - barRadius;

                            // Nếu khoảng cách này < minCover thì tính toán con số bị lẹm
                            if (clearDistance < minCover)
                            {
                                double violation = minCover - clearDistance;
                                if (violation > maxViolation) maxViolation = violation;
                            }
                        }
                    }
                }
            }
            // Chuyển đổi từ Feet sang Millimeters trước khi trả về
            return UnitUtils.ConvertFromInternalUnits(maxViolation, UnitTypeId.Millimeters);
        }

        private static double GetHostMinimumCover(Element host)
        {
            double minCover = double.MaxValue;

            // Các biến quy định Cover mặc định trong Revit
            BuiltInParameter[] coverParams = new BuiltInParameter[]
            {
                BuiltInParameter.CLEAR_COVER_TOP,
                BuiltInParameter.CLEAR_COVER_BOTTOM,
                BuiltInParameter.CLEAR_COVER_OTHER,
                BuiltInParameter.CLEAR_COVER_EXTERIOR,
                BuiltInParameter.CLEAR_COVER_INTERIOR
            };

            foreach (var paramId in coverParams)
            {
                Parameter p = host.get_Parameter(paramId);
                if (p != null && p.HasValue)
                {
                    ElementId coverTypeId = p.AsElementId();
                    if (coverTypeId != ElementId.InvalidElementId)
                    {
                        if (host.Document.GetElement(coverTypeId) is RebarCoverType coverType)
                        {
                            if (coverType.CoverDistance < minCover)
                            {
                                minCover = coverType.CoverDistance;
                            }
                        }
                    }
                }
            }

            return minCover == double.MaxValue ? 0 : minCover;
        }

        private static Solid GetLargestSolid(GeometryElement geomElem)
        {
            Solid largestSolid = null;
            double maxVolume = 0;

            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid && solid.Volume > 0)
                {
                    if (solid.Volume > maxVolume)
                    {
                        maxVolume = solid.Volume;
                        largestSolid = solid;
                    }
                }
                else if (geomObj is GeometryInstance geomInst)
                {
                    GeometryElement instGeom = geomInst.GetInstanceGeometry();
                    Solid instSolid = GetLargestSolid(instGeom);
                    if (instSolid != null && instSolid.Volume > maxVolume)
                    {
                        maxVolume = instSolid.Volume;
                        largestSolid = instSolid;
                    }
                }
            }
            return largestSolid;
        }
    }
}