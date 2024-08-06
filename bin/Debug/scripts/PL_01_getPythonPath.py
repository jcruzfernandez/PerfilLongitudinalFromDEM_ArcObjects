import sys
reload(sys)
import os
import arcpy
sys.setdefaultencoding("utf-8")

def getPythonPath():
    pydir = sys.exec_prefix
    pyexe = os.path.join(pydir, "python.exe")
    if os.path.exists(pyexe):
        return pyexe
    else:
        raise RuntimeError("No python.exe found in {0}".format(pydir))

if __name__ == "__main__":
    pyexe = getPythonPath()
    arcpy.AddMessage("Python Path: {0}".format(pyexe))
    arcpy.SetParameterAsText(0, pyexe)