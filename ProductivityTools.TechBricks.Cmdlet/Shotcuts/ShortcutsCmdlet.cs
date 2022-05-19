using ProductivityTools.TechBricks.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace ProductivityTools.TechBricks.Cmdlet.Shotcuts
{
    [Cmdlet(VerbsCommon.Get,"Shortcuts")]
    public class ShortcutsCmdlet :PSCmdlet.PSCmdletPT
    {
        protected override async void ProcessRecord()
        {
            WriteOutput("HEllo");
            FirebaseAccess fb = new FirebaseAccess();
            await fb.Get();
            base.ProcessRecord();
        }
    }
}
