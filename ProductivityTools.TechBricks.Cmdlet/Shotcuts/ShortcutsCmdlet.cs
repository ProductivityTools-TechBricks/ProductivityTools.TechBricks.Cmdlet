using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace ProductivityTools.TechBricks.Cmdlet.Shotcuts
{
    [Cmdlet(VerbsCommon.Get,"Shortcuts")]
    internal class ShortcutsCmdlet :PSCmdlet.PSCmdletPT
    {
        protected override void ProcessRecord()
        {
            WriteOutput("HEllo");
            base.ProcessRecord();
        }
    }
}
