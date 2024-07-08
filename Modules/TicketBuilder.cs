using WebSupergoo.ABCpdf12;

namespace DCElectricWebAPI.Modules
{
    public class TicketBuilder
    {

        TicketBuilder()
        {
            var isValid = XSettings.InstallLicense(@"[X/VKS0cPn5FgsCJaaK+NbIL+Lb9IQ4MYlq3wxL3
FA0ojxkiVPH3rYMVWQ0lkwg8KCtYw4j5AuSAdr6I
mQbV9xFMgfGSVBH423zFMO/XgBjbi1y7S5MlUFrj
UWBKMcmImUL1oUMFb8wtwCFVMoSiSIEERXiebQ2W
5r+Qn81U4T+/CpdgBuze3yXnWsbpWyRJddxMW83l
QxH+Ofn0BHagRllgNxXXMN6ZhZPv+MFaRTODAdik
liL6wAD07iDOwYuA=]");

        }

        public void BuildTicket()
        {


            /* Generate a Doc object */
            Doc doc = new Doc();


        }
    }
}
