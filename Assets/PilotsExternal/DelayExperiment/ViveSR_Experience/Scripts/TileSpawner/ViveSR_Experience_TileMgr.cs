using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_TileMgr : MonoBehaviour
    {
        List<ViveSR_Experience_Tile> tileList = new List<ViveSR_Experience_Tile>();

        public void AddTile(GameObject gb)
        {
            ViveSR_Experience_Tile tile = gb.GetComponent<ViveSR_Experience_Tile>();
            if(tile)
                tileList.Add(tile);
        }

        //public void RemoveTile(GameObject gb)
        //{
        //    ViveSR_Tile tile = gb.GetComponent<ViveSR_Tile>();
        //    if (tile)
        //    {
        //        tileList.Remove(tile);
        //        Destroy(gb);
        //    }
        //}

        public void RemoveAllTiles()
        {
            foreach (ViveSR_Experience_Tile tile in tileList)
            {
                Destroy(tile.gameObject);
            }
            tileList.Clear();
        }
    }
}