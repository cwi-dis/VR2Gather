//  © - 2020 – viaccess orca 
//  
//  Copyright
//  This code is strictly confidential and the receiver is obliged to use it 
//  exclusively for his or her own purposes. No part of Viaccess-Orca code may
//  be reproduced or transmitted in any form or by any means, electronic or 
//  mechanical, including photocopying, recording, or by any information 
//  storage and retrieval system, without permission in writing from 
//  Viaccess S.A. The information in this code is subject to change without 
//  notice. Viaccess S.A. does not warrant that this code is error-free. If 
//  you find any problems with this code or wish to make comments, please 
//  report them to Viaccess-Orca.
//  
//  Trademarks
//  Viaccess-Orca is a registered trademark of Viaccess S.A in France and/or
//  other countries. All other product and company names mentioned herein are
//  the trademarks of their respective owners. Viaccess S.A may hold patents,
//  patent applications, trademarks, copyrights or other intellectual property
//  rights over the code hereafter. Unless expressly specified otherwise in a 
//  written license agreement, the delivery of this code does not imply the 
//  concession of any license over these patents, trademarks, copyrights or 
//  other intellectual property.

using System.Collections.Generic;
using Best.HTTP.JSON.LitJson;
using System.Reflection;

namespace VRT.Orchestrator.Wrapping
{
    public static class Helper
    {
        // Parse JsonData and returns the appropriate element
        public static List<T> ParseElementsList<T>(JsonData dataList) where T : OrchestratorElement
        {
            List<T> list = new List<T>();
            for (int i = 0; i < dataList.Count; i++)
            {
                T element = OrchestratorElement.ParseJsonData<T>(dataList[i]);
                list.Add(element);
            }
            return list;
        }

        public static double GetClockTimestamp(System.DateTime pDate)
        {
            return pDate.Subtract(new System.DateTime(1970, 1, 1)).TotalSeconds;
        }
    }
}