import sys
reload(sys)
sys.setdefaultencoding("utf-8")
# Generacion del Perfil Longitudinal a partir de 
# la linea de seccion y Modelo de Elevacion Digital (DEM).
# Esta herramienta solo es funcional dentro de una
# sesion de Arcmap 

# Importacion de librerias
from osgeo import gdal
import arcpy
import os
import json
import math
# import pandas as pd
# import numpy as np
from arcpy.sa import *
import shp_template_json as auttmp
import traceback

# Habilitando la sobreescritura
arcpy.env.overwriteOutput = True

# Definiendo parametros
raster_dem = arcpy.GetParameterAsText(0)
linestring_wkt = arcpy.GetParameterAsText(1)
wkid_code = arcpy.GetParameterAsText(2)
tolerancia = arcpy.GetParameterAsText(3) # aqui tambien llega la Letra de inicio y fin de perfil
x_Ini = float(arcpy.GetParameterAsText(4))
y_Ini = float(arcpy.GetParameterAsText(5))
h_ini = 0#int(arcpy.GetParameterAsText(7))
_MARGEN_INFERIOR = 8000

# Objeto a retornar
response = dict()
response['status'] = 1
response['message'] = 'success'

# EPSG
wkid = int(wkid_code) #int('327{}'.format(zona))

# Puntos cardinales ordenados por cuadrante
puntos_cardinales = [['SO', 'NE'], ['SE', 'NO'], ['NE', 'SO'], ['NO', 'SE']]

def get_z_value_by_arcpy(utm_x, utm_y):
    # raster = arcpy.Raster(raster_dem)
    z_value = arcpy.GetCellValue_management(raster_dem, "{} {}".format(utm_x,utm_y),"1").getOutput(0)

    return int(float(z_value))

# Procesamiento
try:
    # Documento de mapa actual
    mxd = arcpy.mapping.MapDocument('CURRENT')

    # Obteniendo el sistema de referencia (src) del modelo de elevacion digital (DEM)
    raster_dem_src = arcpy.Describe(raster_dem).spatialReference

    if raster_dem_src.factoryCode not in (32717, 32718, 32719):
        raise RuntimeError("ERROR EN EL SISTEMA DE REFERENCIA DEL DEM")

    # Ubicacion de features inputs
    # Conversion de la linea de seccion de wkt a geometry
    linestring_geom = arcpy.FromWKT(linestring_wkt, mxd.activeDataFrame.spatialReference)

    # Coordenada inicial para graficacion del perfil
    y_ini = y_Ini #ymin_cuad - _MARGEN_INFERIOR

    # Punto de altura máxima que puede tomar el perfil
    y_top = 0

    # :PROCESO DE CAPTURA DE VALORES Z DEL DEM

    # Si el DEM y el dataframe actual no tienen el mismo SRC, es necesario reproyectar 
    # la linea de seccion
    if raster_dem_src.factoryCode != mxd.activeDataFrame.spatialReference.factoryCode:
        linestring_geom = linestring_geom.projectAs(raster_dem_src)

    # Accediento a los datos del DEM
    raster = gdal.Open(raster_dem)
    gt = raster.GetGeoTransform()               # necesario para obtencion de valores z

    width_resolution = gt[1]                    # resolucion horizontal
    heigth_resolution = gt[-1]                  # resolucion vertical

    coords = list()

    # Se obtienen todas las divisiones de longitud igual a la resolucion horizontal
    #  a los largo de la longitud de la linea de seccion
    iterable = range(0, int(linestring_geom.length), int(width_resolution))
    iterable.append(linestring_geom.length)

    first_point = linestring_geom.firstPoint    # punto inicial de la linea de seccion
    end_point = linestring_geom.lastPoint       # punto final de la linea de seccion

    for i, r in enumerate(iterable, 1):
        # se obtiene el punto a una distancia width_resolution del punto inicial de la linea de seccion
        pnt = linestring_geom.positionAlongLine(r, False)
        x, y = pnt.centroid.X, pnt.centroid.Y

        raster_x = int((x - gt[0]) / gt[1])     # coordenada X equivalentes en el DEM
        raster_y = int((y - gt[3]) / gt[5])     # coordenada Y equivalentes en el DEM

        # Se obtiene el valor de z a partir de las coordenadas equivalentes
        try:
            z_arr = raster.GetRasterBand(1).ReadAsArray(raster_x, raster_y, 1, 1)
            z = z_arr[0][0] + y_ini - h_ini
            # Se captura el valor con mayor altura
            if z_arr[0][0] - h_ini > y_top:
                y_top = z_arr[0][0] - h_ini
        except:
            #z_arr = raster.GetRasterBand(1).ReadRaster(raster_x, raster_y, 1, 1, buf_type=gdal.GDT_Float32)
            z_arr = get_z_value_by_arcpy(x,y)
            z = z_arr + y_ini - h_ini
            # Se captura el valor con mayor altura
            if z_arr - h_ini > y_top:
                y_top = z_arr - h_ini

        # Se almacena el valor z en una lista
        coords.append('{} {}'.format(x_Ini + r, z))

    rows = list()

    coords.sort(key=lambda i: i.split(' ')[0])

    y_top = y_top * 1.2 + y_ini

    sec_lines_string = ','.join(coords)
    wkt = "LINESTRING ({})".format(sec_lines_string)

    geometry_line = arcpy.FromWKT(wkt)
    geometry_line_JSON= json.loads(geometry_line.JSON)

    auttmp._PL_PERFIL_TEMPLATE['features'] = []

    data = {
        "attributes": {
            "LABEL": 'PERFIL',
            "VALUE": geometry_line.length
        }, 
        "geometry": {
            "paths": geometry_line_JSON['paths']
        }
    }
    auttmp._PL_PERFIL_TEMPLATE['features'].append(data)

    # Linea horizontal (base)
    linea_seccion = {
                    "attributes": {
                        "LABEL": 'LONGITUD EJE',
                        "VALUE": linestring_geom.length
                    }, 
                    "geometry": {
                        "paths": [[[x_Ini, y_ini], [x_Ini + linestring_geom.length, y_ini]]] 
                    }
                }
    auttmp._PL_PERFIL_TEMPLATE['features'].append(linea_seccion)

    # Linea de altitud A (IZQUIERDO)
    linea_altura1 = {
                    "attributes": {
                        "LABEL": 'ALTITUD-IZQUIERDA',
                        "VALUE": y_top - y_ini
                    }, 
                    "geometry": {
                        "paths": [[[x_Ini, y_top], [x_Ini, y_ini]]]
                    }
                }
    auttmp._PL_PERFIL_TEMPLATE['features'].append(linea_altura1)

    # Linea de altitud A' (DERECHO)
    linea_altura2 = {
                    "attributes": {
                        "LABEL": "ALTITUD-DERECHA",
                        "VALUE": y_top - y_ini
                    }, 
                    "geometry": {
                        "paths": [[[x_Ini + linestring_geom.length, y_top], [x_Ini + linestring_geom.length, y_ini]]]
                    }
                }
    auttmp._PL_PERFIL_TEMPLATE['features'].append(linea_altura2)

    # Altura total del perfil
    htot = int(math.ceil(y_top - y_ini))

    for i in  range(h_ini, htot):
        if i % 500 != 0:
            continue
        y_mark = y_ini + abs(h_ini - i)
        if y_mark > y_top:
            continue
        xA_ini = x_Ini #first_point.X <> x_Ini
        xA_end = xA_ini - 100
        xB_ini = x_Ini + linestring_geom.length #first_point.X <> x_Ini
        xB_end = xB_ini + 100
        
        # Marcas de altitud en linea de altitud A
        hmark_line = {
                    "attributes": {
                        "LABEL": "MARCADOR IZQUIERDO",
                        "VALUE": i
                    }, 
                    "geometry": {
                        "paths": [[[xA_ini, y_mark], [xA_end, y_mark]]]
                    }
                }
        auttmp._PL_PERFIL_TEMPLATE['features'].append(hmark_line)

        # Marcas de altitud en linea de altitud A'
        hmark_line2 = {
                    "attributes": {
                        "LABEL": "MARCADOR DERECHO",
                        "VALUE": i
                    }, 
                    "geometry": {
                        "paths": [[[xB_ini, y_mark], [xB_end, y_mark]]]
                    }
                }
        auttmp._PL_PERFIL_TEMPLATE['features'].append(hmark_line2)

    # Cargando las linea de perfil a la base de datos
    rows_seccion = arcpy.AsShape(auttmp._PL_PERFIL_TEMPLATE, True)
    
    # Definir el nombre del FeatureClass en memoria
    in_memory_fc = "in_memory\\Perfil_Longitudinal"

    # Crear un FeatureClass en memoria con un campo de ID y un campo de geometría
    arcpy.CreateFeatureclass_management("in_memory", "Perfil_Longitudinal", "POLYLINE")
    arcpy.AddField_management(in_memory_fc, "LABEL", "TEXT", "", "", "100", "ETIQUETA","NON_NULLABLE","NON_REQUIRED","")
    arcpy.AddField_management(in_memory_fc, "VALUE", "DOUBLE", "", "", "", "VALOR", "NON_NULLABLE", "NON_REQUIRED", "")
    # Agregando capa de seccion al mapa
    
    arcpy.Append_management(rows_seccion, in_memory_fc, "NO_TEST")

    # Etiquetas de Marcas de altitud - show
    mxd2 = arcpy.mapping.MapDocument("CURRENT")
    dfs = arcpy.mapping.ListDataFrames(mxd2)[0]
    # Crear una capa a partir del FeatureClass en memoria
    layer = arcpy.mapping.Layer(in_memory_fc)

    # Agregar la capa al dataframe de ArcMap
    arcpy.mapping.AddLayer(dfs, layer, "TOP")
    
    response['response'] = True
except Exception as e:
    response['status'] = 0
    response['message'] = traceback.format_exc()#e.message
finally:
    response = json.dumps(response)
    arcpy.RefreshTOC()
    arcpy.RefreshActiveView()
    arcpy.SetParameterAsText(6, response)