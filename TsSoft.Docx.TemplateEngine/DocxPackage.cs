﻿using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace TsSoft.Docx.TemplateEngine
{
    internal class DocxPackage
    {
        private const string OfficeDocumentRelType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
        private Stream docxStream;

        public DocxPackage()
        {
        }

        public DocxPackage(Stream docxStream)
        {
            this.docxStream = docxStream;
        }

        public virtual XDocument DocumentPartXml { get; private set; }

        public virtual DocxPackage Load()
        {
            this.docxStream.Seek(0, SeekOrigin.Begin);
            using (Package package = Package.Open(this.docxStream, FileMode.Open, FileAccess.Read))
            {
                var docPart = this.GetDocumentPart(package);
                using (XmlReader reader = XmlReader.Create(docPart.GetStream()))
                {
                    this.DocumentPartXml = XDocument.Load(reader);
                }
            }
            return this;
        }

        public virtual DocxPackage Save()
        {
            this.docxStream.Seek(0, SeekOrigin.Begin);
            using (Package package = Package.Open(this.docxStream, FileMode.Open, FileAccess.ReadWrite))
            {
                var docPart = this.GetDocumentPart(package);
                var documentStream = docPart.GetStream();
                documentStream.SetLength(0);
                using (var writer = new XmlTextWriter(documentStream, new UTF8Encoding()))
                {
                    this.DocumentPartXml.Save(writer);
                }
                package.Flush();
            }
            return this;
        }

        private PackagePart GetDocumentPart(Package package)
        {
            PackageRelationship relationship = package.GetRelationshipsByType(OfficeDocumentRelType).FirstOrDefault();
            Uri docUri = PackUriHelper.ResolvePartUri(new Uri("/", UriKind.Relative), relationship.TargetUri);
            return package.GetPart(docUri);
        }
    }
}