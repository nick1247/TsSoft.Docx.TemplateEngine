﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace TsSoft.Docx.TemplateEngine.Tags.Processors
{
    internal class TableProcessor : AbstractProcessor
    {
        public TableTag TableTag { get; set; }

        public override void Process()
        {
            base.Process();

            if (TableTag == null)
            {
                throw new NullReferenceException();
            }

            var tableRows = TableTag.Table.Elements(WordMl.TableRowName);
            var dynamicRow = TableTag.DynamicRow.HasValue ? tableRows.ElementAt(TableTag.DynamicRow.Value - 1) : tableRows.Last();

            ReplaceValues(dynamicRow);

            RemoveTags();
        }

        private void ReplaceValues(XElement dynamicRow)
        {
            var readers = DataReader.GetReaders(TableTag.ItemsSource);
            int rowIndex = 1;
            foreach (var reader in readers)
            {
                var currentRow = new XElement(dynamicRow);
                IList<XElement> rowTagElements = currentRow.Elements(WordMl.SdtName).ToList();
                for (int index = 0; index < rowTagElements.Count; index++)
                {
                    var cellTag = rowTagElements[index];
                    ReplaceValue(reader, cellTag, rowIndex);
                }
                rowIndex++;

                dynamicRow.AddBeforeSelf(currentRow);
            }
            dynamicRow.Remove();
        }

        private void ReplaceValue(DataReader reader, XElement cellTag, int rowIndex)
        {
            var cell = cellTag.Element(WordMl.SdtContentName).Element(WordMl.TableCellName);
            var tagNameElement = cellTag.Element(WordMl.SdtPrName).Element(WordMl.TagName);
            var cellTagType = tagNameElement.Attribute(WordMl.ValAttributeName).Value;
            string replacementValue = string.Empty;
            switch (cellTagType)
            {
                case "ItemIndex":
                    replacementValue = rowIndex.ToString();
                    break;
                case "Item":
                    var itemPath = cellTag.Value;
                    replacementValue = reader.ReadText(itemPath);
                    break;
            }
            cell.Element(WordMl.ParagraphName).Elements(WordMl.TextRunName).Remove();
            var lastPChild = cell.Element(WordMl.ParagraphName).Elements().LastOrDefault();
            var cellTextElement = DocxHelper.CreateTextElement(cell, cell.Element(WordMl.ParagraphName), replacementValue);
            if (lastPChild == null)
            {
                cell.Element(WordMl.ParagraphName).Add(cellTextElement);
            }
            else
            {
                lastPChild.AddAfterSelf(cellTextElement);
            }
            cellTag.AddAfterSelf(cell);
            cellTag.Remove();
        }

        private void RemoveTags()
        {
            this.CleanUp(TableTag.TagTable, TableTag.TagContent);
            this.CleanUp(TableTag.TagEndContent, TableTag.TagEndTable);
        }
               

    }
}
