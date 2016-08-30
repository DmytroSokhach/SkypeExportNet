using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkypeExportNet.Exporter.ReportModel
{
    public class AccountReport
    {
        public string Name;
        public List<ConversationReport> Conversations = new List<ConversationReport>();
    }
}
